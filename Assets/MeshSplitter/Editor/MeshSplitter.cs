using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MeshSplitterEditor : EditorWindow
{
	private GameObject _target_object;
	private MeshFilter _quad_filter;

	[MenuItem ("MeshSplitter/MeshSplitter Editor")]
	private static void Create ()
	{
		GetWindow<MeshSplitterEditor> ("MeshSplitter");
	}

	private void OnGUI ()
	{
		EditorGUI.BeginChangeCheck ();
		_target_object = EditorGUILayout.ObjectField ("Target Object", _target_object, typeof(GameObject), true) as GameObject;
		EditorGUILayout.Space ();

		_quad_filter = EditorGUILayout.ObjectField ("Quad", _quad_filter, typeof(MeshFilter), true) as MeshFilter;
		EditorGUILayout.Space ();

		if (EditorGUI.EndChangeCheck ()) {
		}

		if (_quad_filter != null && _quad_filter.sharedMesh.name != "Quad") {
			_quad_filter = null;
		}

		if (_target_object != null) {
			bool hasMesh = _target_object.GetComponent<MeshFilter>() != null || _target_object.GetComponent<SkinnedMeshRenderer>() != null;

			if (!hasMesh) {
				EditorGUILayout.HelpBox ("Selected object has no MeshFilter or SkinnedMeshRenderer", MessageType.Warning);
			} else {
				if (GUILayout.Button ("split by materials")) {
					SplitByMaterials (_target_object);
				}
				EditorGUILayout.Space ();

				if (_quad_filter != null) {
					if (GUILayout.Button ("split by quad")) {
						splitByQuad (_target_object, _quad_filter);
					}
				}
			}
		}

	}

	private void SplitByMaterials (GameObject targetObject)
	{
		Mesh mesh;
		Material[] materials;
		string submesh_dir;

		var meshFilter = targetObject.GetComponent<MeshFilter>();
		var skinnedMesh = targetObject.GetComponent<SkinnedMeshRenderer>();

		if (meshFilter != null) {
			mesh = meshFilter.sharedMesh;
			materials = targetObject.GetComponent<MeshRenderer>()?.sharedMaterials;
			submesh_dir = getSubmeshPath (mesh);
		} else if (skinnedMesh != null) {
			mesh = skinnedMesh.sharedMesh;
			materials = skinnedMesh.sharedMaterials;
			submesh_dir = getSubmeshPath (mesh);
		} else {
			return;
		}

		createFolder (submesh_dir);

		for (int i = 0; i < mesh.subMeshCount; i++) {
			splitSubmesh (targetObject, mesh, materials, i, submesh_dir);
		}

		targetObject.SetActive (false);
	}

	private string getSubmeshPath (Mesh mesh)
	{
		string mesh_path = AssetDatabase.GetAssetPath (mesh);
		string base_dir = Path.GetDirectoryName (mesh_path);

		if (base_dir.EndsWith ("Submeshes")) {
			return base_dir;
		} else {
			return Path.Combine (base_dir, "Submeshes");
		}
	}

	private void createFolder (string path)
	{
		if (AssetDatabase.IsValidFolder (path)) {
			return;
		}

		string parent = Path.GetDirectoryName (path);
		string dirname = Path.GetFileName (path);

		if (!AssetDatabase.IsValidFolder (parent)) {
			createFolder (parent);
		}

		AssetDatabase.CreateFolder (parent, dirname);
	}

	private void splitSubmesh (GameObject targetObject, Mesh mesh, Material[] materials, int index, string submesh_dir)
	{
		string material_name = materials [index].name;
		string mesh_name = targetObject.name + "_" + material_name;
		var triangles = new int[][] { mesh.GetTriangles (index) };

		createNewMesh (targetObject, mesh, materials, triangles, submesh_dir, mesh_name, index);
	}

	private GameObject cloneObject (GameObject gameObject)
	{
		return Instantiate (gameObject, gameObject.transform.parent) as GameObject;
	}

	private void splitByQuad (GameObject targetObject, MeshFilter mesh_filter)
	{
		var plane = createPlane (mesh_filter);
		Mesh mesh;
		Material[] materials;

		var meshFilter = targetObject.GetComponent<MeshFilter>();
		var skinnedMesh = targetObject.GetComponent<SkinnedMeshRenderer>();

		if (meshFilter != null) {
			mesh = meshFilter.sharedMesh;
			materials = targetObject.GetComponent<MeshRenderer>()?.sharedMaterials;
		} else if (skinnedMesh != null) {
			mesh = skinnedMesh.sharedMesh;
			materials = skinnedMesh.sharedMaterials;
		} else {
			return;
		}

		var matrix = targetObject.transform.localToWorldMatrix;

		string mesh_name = targetObject.name;
		string submesh_dir = getSubmeshPath (mesh);
		createFolder (submesh_dir);

		var tri_a = new List<List<int>> ();
		var tri_b = new List<List<int>> ();

		for (int j = 0; j < mesh.subMeshCount; j++) {
			var triangles = mesh.GetTriangles (j);
			tri_a.Add (new List<int> ());
			tri_b.Add (new List<int> ());

			for (int i = 0; i < triangles.Length; i += 3) {
				var triangle = triangles.Skip (i).Take (3);
				bool side = false;

				foreach (int n in triangle) {
					side = side || plane.GetSide (matrix.MultiplyPoint (mesh.vertices [n]));
				}

				if (side) {
					tri_a [j].AddRange (triangle);
				} else {
					tri_b [j].AddRange (triangle);
				}

				if (i % 30 == 0) {
					EditorUtility.DisplayProgressBar (
						"処理中",
						string.Format ("submesh:{0}/{1}, triangles:{2}/{3}", j, mesh.subMeshCount, i, triangles.Length),
						(float)i / triangles.Length
					);
				}
			}
		}

		createNewMesh (targetObject, mesh, materials, tri_a.Select (n => n.ToArray ()).ToArray (), submesh_dir, mesh_name + "_a", -1);
		createNewMesh (targetObject, mesh, materials, tri_b.Select (n => n.ToArray ()).ToArray (), submesh_dir, mesh_name + "_b", -1);
		targetObject.SetActive (false);

		EditorUtility.ClearProgressBar ();
	}

	private void createNewMesh (GameObject original, Mesh originalMesh, Material[] originalMaterials, int[][] triangles, string dirname, string name, int materialIndex)
	{
		var gameObject = cloneObject (original);
		gameObject.name = name;

		var meshFilter = gameObject.GetComponent<MeshFilter>();
		var skinnedMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
		var meshRenderer = gameObject.GetComponent<MeshRenderer>();

		var mesh = Instantiate (originalMesh) as Mesh;

		mesh.subMeshCount = triangles.Length;
		for (int i = 0; i < triangles.Length; i++) {
			mesh.SetTriangles (triangles [i], i);
		}
		AssetDatabase.CreateAsset (mesh, Path.Combine (dirname, name + ".asset"));

		if (meshFilter != null) {
			meshFilter.sharedMesh = mesh;
			if (materialIndex >= 0 && meshRenderer != null) {
				meshRenderer.sharedMaterials = new Material[] { originalMaterials [materialIndex] };
			}
		} else if (skinnedMesh != null) {
			skinnedMesh.sharedMesh = mesh;
			if (materialIndex >= 0) {
				skinnedMesh.sharedMaterials = new Material[] { originalMaterials [materialIndex] };
			}
		}
	}

	private Plane createPlane (MeshFilter mesh_filter)
	{
		var matrix = mesh_filter.transform.localToWorldMatrix;
		var mesh = mesh_filter.sharedMesh;
		var vertices = mesh.triangles.Take (3).Select (n => matrix.MultiplyPoint (mesh.vertices [n])).ToArray ();
		return new Plane (vertices [0], vertices [1], vertices [2]);
	}
}
