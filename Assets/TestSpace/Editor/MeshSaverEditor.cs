using UnityEditor;
using UnityEngine;







public static class MeshSaverEditor {

    // Creates a new menu item 'Examples > Create Prefab' in the main menu.
    [MenuItem("CONTEXT/MeshGen/Create Prefab")]
    private static void CreatePrefab()
        {
            
            // Keep track of the currently selected GameObject(s)
            GameObject[] objectArray = Selection.gameObjects;

            // Loop through every GameObject in the array above
            foreach (GameObject gameObject in objectArray)
            {
                // Set the path as within the Assets folder,
                // and name it as the GameObject's name with the .Prefab format
                string localPath = "Assets/TestSpace/" + gameObject.name + ".prefab";

                // Make sure the file name is unique, in case an existing Prefab has the same name.
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

                
                // Create the new Prefab.
                PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction);
            }
        }
    
    
    [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
    public static void SaveMeshInPlace (MenuCommand menuCommand) {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, false, true);
        }

    [MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
    public static void SaveMeshNewInstanceItem (MenuCommand menuCommand) {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, true, true);
        }

    public static void SaveMesh (Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
            string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
		
            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);
        
            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }
	
}