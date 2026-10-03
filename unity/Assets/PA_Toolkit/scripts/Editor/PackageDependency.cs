#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Collections.Generic;

namespace PA_Toolkit
{
    public static class PackageDependency
    {
        static ListRequest listRequest;
        static AddRequest addRequest;

        static string searchingPackage;
        static string searchingUrl;

        static Queue<string> searchingPackageQueue = new Queue<string>();
        static Queue<string> searchingUrlQueue = new Queue<string>();
        static bool activeSearch => listRequest != null || addRequest != null;
        public static void CheckForPackage(string packageName, string githubURL)
        {
            if (activeSearch)
            {

                AddToQueue(packageName, githubURL);
                return;
            }
            searchingPackage = packageName;
            searchingUrl = githubURL;
            listRequest = Client.List();
            EditorApplication.update += HasPackage;
        }

        static void HasPackage()
        {
            if (listRequest.IsCompleted)
            {
                bool included = false;
                foreach (var package in listRequest.Result)
                {
                    if (package.name.Equals(searchingPackage))
                    {
                        included = true;
                        break;
                    }
                }
                if (!included)
                {
                    bool shouldImport = EditorUtility.DisplayDialog(
                    "Package Dependency",
                    "PA_Toolkit depends on the following package:\n\n" + searchingPackage,
                    "Import Package",
                    "Cancel");

                    if (shouldImport)
                    {
                        string source = searchingUrl.Equals("") ? searchingPackage : searchingUrl;
                        addRequest = Client.Add(source);
                        EditorApplication.update += AddPackage;
                    }
                }

                listRequest = null;
                EditorApplication.update -= HasPackage;
            }

        }

        static void AddPackage()
        {
            if (addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                    Debug.Log($"Installed: {addRequest.Result.packageId}\nPlease restart the editor");
                else if (addRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(addRequest.Error.message);

                }

                EditorApplication.update -= AddPackage;
                addRequest = null;
                searchingPackage = null;


                if (searchingPackageQueue.Count == 0)
                {
                    bool restart = EditorUtility.DisplayDialog(
                    "Package Dependency",
                    "Issues can occur if the editor is not restarted, would you like to restart the Unity Editor?",
                    "Restart",
                    "Cancel");
                    if (restart)
                    {

                        string projectPath = Application.dataPath.Remove(
                    Application.dataPath.Length - "/Assets".Length, "/Assets".Length);
                        EditorApplication.OpenProject(projectPath);

                    }
                }
            }
        }

        static void AddToQueue(string packageName, string url)
        {
            if (searchingPackageQueue.Contains(packageName)) return;

            searchingPackageQueue.Enqueue(packageName);
            searchingUrlQueue.Enqueue(url);
            if (searchingPackageQueue.Count > 0)
            {
                EditorApplication.update += UnloadQueue;
            }


        }

        static void UnloadQueue()
        {
            if (!activeSearch)
            {
                if (searchingPackageQueue.Count > 0) CheckForPackage(searchingPackageQueue.Dequeue(), searchingUrlQueue.Dequeue());
                else if (searchingPackageQueue.Count == 0)
                {
                    EditorApplication.update -= UnloadQueue;
                }
            }
        }
    }
}
#endif