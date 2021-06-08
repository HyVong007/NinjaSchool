using UnityEditor;
using UnityEngine;


namespace NinjaSchool.Editor
{
	internal static class Menu
	{
		[MenuItem("Tools/Create Prefabs")]
		private static void CreatePrefabs()
		{
			foreach (var gameObject in Selection.gameObjects)
			{
				string localPath = "Assets/" + gameObject.name + ".prefab";
				localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
				PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction);
			}
			Debug.Log("All prefabs are created successfully !");
		}


		[MenuItem("Tools/Create Prefabs", true)]
		private static bool ValidateCreatePrefabs() =>
			Selection.activeGameObject && !EditorUtility.IsPersistent(Selection.activeGameObject);
	}
}