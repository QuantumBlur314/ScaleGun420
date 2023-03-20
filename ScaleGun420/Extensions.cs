﻿using OWML.ModHelper.Events;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineInternal;


namespace ScaleGun420
{
    public static class Extensions
    {
        public static string GetPath(this Transform current)    //Xen says New Horizons uses this.  It has to be in a static class, so that's why it's in Extensions
        {
            if (current.parent == null) return current.name;
            ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"{current}");
            return current.parent.GetPath() + "/" + current.name; //burrows UP
        }

        //myCoolCreateChildMethod.cs mangled and mingled with Vio's
        //btw the first parameter being "this" makes sure it references whatever thingy is calling the method, and makes that first parameter not need to be manually defined if the method is after "thingdoingthemethod.SpawnChildGOAtParent"
        public static GameObject GivesBirthTo(this GameObject parentGO, string childName, bool spawnsActive, Vector3 localPositionHaha = default, Vector3 localEulerAnglesHaha = default, float scaleMultiplier = 1)
        {
            GameObject childObj = new(childName);  //the parentheses in new Gameobject() are ALREADY MADE to house its name as a string, no quotation marks needed (except maybe in calling the method idk yet)

            Transform parentTransform = parentGO.GetComponent<Transform>();
            childObj.transform.SetParent(parentTransform);

            childObj.transform.localPosition = localPositionHaha;  //DO NOT COPY THE localANYTHING FROM THE PARENT BTW, it will "add up"
            childObj.transform.localEulerAngles = localEulerAnglesHaha;
            childObj.transform.localScale = scaleMultiplier * Vector3.one;
            childObj.SetActive(spawnsActive);
            return childObj;
        }


        //lots of these can be handled by just AddComponent<>() WHICH IS, ITSELF, A GENERIC BTW



        //VIO'S BETTER CreateChild:  (also inadvertently led to Idiot teaching Vio about extensions, I FINALLY KNEW ABOUT SOMETHING BEFORE VIO DID {arbitrarily, because Xen told me about extensions})
        //"Vector3 localPosition = default" sets the child to parent's 0,0,0 by default, tweak if you want, BUT DON'T HAVE TO.  Those "=" and "defaults" in the parentheses set default if you don't need them custom when calling
        public static GameObject CreateChild(this Transform parentTransform, string name, bool spawnsActive, Vector3 localPosition = default, Vector3 localEulerAngles = default, float scaleMultiplier = 1)
        {
            var childObj = new GameObject(name);               //naming the local Vector vars the same name as the .transform.localPosition (etc.) guys DOES NOT MATTER ACTUALLY AUGH
            childObj.transform.SetParent(parentTransform);
            childObj.transform.localPosition = localPosition;      //once the parent is set, the Vector3's are already relative to the parent object with the =default stuff
            childObj.transform.localEulerAngles = localEulerAngles;
            childObj.transform.localScale = scaleMultiplier * Vector3.one;
            childObj.SetActive(spawnsActive);
            return childObj;
        }
        public static GameObject InstantiatePrefab(this GameObject parentGO, string streamingAssetsBath, string prefabBath, bool spawnsActive, Vector3 localPosition = default, Vector3 localEulerAngles = default)
        {
            LoadPrefab();
            GameObject newPrefab = ScaleGun420Modbehavior.Instantiate(GameObject.Find(prefabBath), parentGO.transform);

            newPrefab.transform.localPosition = localPosition;
            newPrefab.transform.localEulerAngles = localEulerAngles;
            var streamingRenderMeshHandle = newPrefab.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadPrefab;   //031623_2047: I think the Loadstaff might be getting called repeatedly or something, idk, performance is garbage when equipped
            void LoadPrefab() { if (streamingAssetsBath != null) { StreamingManager.LoadStreamingAssets(streamingAssetsBath); } }
            newPrefab.SetActive(spawnsActive);
            return newPrefab;
        }



        //  private static T InstaPrefabAnyType<T>(this Transform parentTransform, string streamingAssetsBath, string prefabBath, bool spawnsActive, Vector3 localPosition = default, Vector3 localEulerAngles = default)
        // where T : UnityEngine.Component
        // {

        //  LoadPrefab();
        //  T newPrefab = ScaleGun420Modbehavior.Instantiate(UnityEngine.Component.Find(prefabBath), parentTransform);

        // newPrefab.transform.localPosition = localPosition;
        // newPrefab.transform.localEulerAngles = localEulerAngles;
        // var streamingRenderMeshHandle = newPrefab.GetComponentInChildren<StreamingRenderMeshHandle>();
        // streamingRenderMeshHandle.OnMeshUnloaded += LoadPrefab;   //031623_2047: I think the Loadstaff might be getting called repeatedly or something, idk, performance is garbage when equipped
        // void LoadPrefab()
        //{ if (streamingAssetsBath != null) { StreamingManager.LoadStreamingAssets(streamingAssetsBath); } }
        //newPrefab.SetActiveOnAwake(spawnsActive);
        //return newPrefab;

        // }

        public static T GetChildComponentByName<T>(this Transform parent, string name) where T : UnityEngine.Component    //shoutout to markroth8 on Feb 22, 2020 on the unity forums for this
        {
            foreach (T component in parent.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == name)
                {
                    return component;
                }
            }
            return null;
        }


    }
}

