using OWML.Common;
using OWML.ModHelper.Events;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
            //ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"{current}");  
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
        public static GameObject InstantiatePrefab(this Transform parentTransform, string streamingAssetsBath, string prefabBath, bool spawnsActive, Vector3 localPosition = default, Vector3 localEulerAngles = default)
        {
            LoadPrefab();
            GameObject newPrefab = ScaleGun420Modbehavior.Instantiate(GameObject.Find(prefabBath), parentTransform);

            newPrefab.transform.localPosition = localPosition;
            newPrefab.transform.localEulerAngles = localEulerAngles;
            var streamingRenderMeshHandle = newPrefab.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadPrefab;   //031623_2047: I think the Loadstaff might be getting called repeatedly or something, idk, performance is garbage when equipped
            void LoadPrefab() { if (streamingAssetsBath != null) { StreamingManager.LoadStreamingAssets(streamingAssetsBath); } }
            newPrefab.SetActive(spawnsActive);
            return newPrefab;
        }

        public static GameObject InstantiateTextObj(this Transform parentTransform, string pathToGO, string customNameOfText,  //CHECK TO SEE WHETHER 
            out Text textComponent, Vector2 localPosition = default, Vector2 sizeDelta = default, HorizontalWrapMode horizontalOverflow = default, bool spawnActive = true)
        {//^^ NOTICE the "ref", this means it will let you put in a field
            GameObject newTextBeast = ScaleGun420Modbehavior.Instantiate(GameObject.Find(pathToGO).transform.gameObject, parentTransform);
            LogGoob.WriteLine($"set newTextBeast to {newTextBeast} on parent of name {parentTransform}");
            textComponent = newTextBeast.transform.GetComponentInChildren<Text>(true);
            LogGoob.WriteLine($"Successfully set textComponent to {textComponent}");
            textComponent.name = customNameOfText;
            LogGoob.WriteLine($"named textComponent to customNameOfText {customNameOfText}");

            textComponent.rectTransform.localPosition = localPosition;
            textComponent.rectTransform.sizeDelta = sizeDelta;
            textComponent.horizontalOverflow = horizontalOverflow;
            newTextBeast.SetActive(spawnActive);
            return newTextBeast;
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


        public static GameObject AdjacentSiblingIn(this int currentIndex, List<GameObject> listToCheck, int toDirection = 1)
        {
            if (currentIndex > listToCheck.Count)
            {
                LogGoob.WriteLine($"AdjacentSiblingsIn Ln126: currentIndex {currentIndex} was out of range of listToCheck.Count {listToCheck.Count}; returning {listToCheck[0]}");
                return listToCheck[0]; }
            else if (listToCheck.Count <= 1)
            {
                LogGoob.WriteLine($"AdjacentSiblingsIn: listToCheck was too small, with a count of {listToCheck.Count}. {currentIndex} is currentindex", MessageType.Warning);
                currentIndex = 0;
                return listToCheck[currentIndex]; //Returns currentIndex, since searching for 0 didn't work.  Also what the fuck why
            }
            else if (listToCheck.Count() > 1)
            {
                var listLength = listToCheck.Count();
                var internalIndex = currentIndex;
                internalIndex += toDirection;
                internalIndex = ((internalIndex > listLength - 1) ? 0 : internalIndex);
                internalIndex = ((internalIndex < 0) ? listLength - 1 : internalIndex);
                var foundObject = listToCheck[internalIndex];
                return foundObject;
            }
            else return listToCheck[0];
        }
        public static List<GameObject> GetAllSiblings(this GameObject gameObject) //If you wanted to put this in another class, you'd get rid of the "this"
        {
            if (gameObject.transform.parent != null)
            {
                var siblings = new List<GameObject>();
                foreach (Transform sister in gameObject.transform.parent)  //IF YOU'RE ALREADY AS HIGH AS YOU CAN GET, THERE'S NO WAY TO FIND SIBLINGS???????
                { siblings.Add(sister.gameObject); }
                return siblings;
            }
            else
            {
                LogGoob.Scream("GetAllSiblings extension found no parent for lineage tracing; putting selected GameObject in lonely list instead.");
                List<GameObject> soloList = new List<GameObject> { gameObject };
                return soloList;
            }
        }
        public static List<GameObject> GetAllChildren(this GameObject parent) //thanks to Corby and Idiot 
        {
            var children = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
            }
            return children;
        }

        //Corby says Linq is cleaner but less readable; Learn about arrays from this, but do not learn from the Linq
        public static GameObject[] GetAllChildrenButLinqAndArrayInstead(this GameObject parent) //the [] tell code that it will be an array.
        {
            return parent.transform.Cast<Transform>().Select(child => child.gameObject).ToArray();
        }

    }
}

