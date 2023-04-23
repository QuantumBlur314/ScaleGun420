using OWML.Common;
using OWML.ModHelper.Events;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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

        public static GameObject InstantiateTextGO_FromPathForSomeReason(this Transform parentTransform, string pathToGO, string customNameOfText,  //CHECK TO SEE WHETHER 
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


        public static Canvas AddCanvasGO(this Transform parentTransform, string gOName = "CanvasGO_ax15", Vector2 rectTranSizeDelta_1400x200dflt = default, float newGOScale = 0.0004f, Vector3 gOLocalPosition = default, Vector3 gOLocalEulerAngles = default)
        {
            if (rectTranSizeDelta_1400x200dflt == default)
                rectTranSizeDelta_1400x200dflt = new Vector2(1400, 200);

            GameObject canvGO_internal = parentTransform.CreateChild(gOName, true, gOLocalPosition, gOLocalEulerAngles, newGOScale);
            Canvas canvasComponent = canvGO_internal.AddComponent<Canvas>();
            canvasComponent.worldCamera = Locator.GetPlayerCamera().mainCamera;
            canvasComponent.renderMode = RenderMode.WorldSpace;

            RectTransform rectTransform_intrnl = canvGO_internal.GetComponent<RectTransform>();
            rectTransform_intrnl.sizeDelta = rectTranSizeDelta_1400x200dflt;

            return canvasComponent;
        }

        public static Text AddTextGO(this Transform parentTransform, string gOName = "TextGO_ax15", Vector2 sizeDelta500x500 = default, int fontSize = 40, string fontPath = "fonts/english - latin/SpaceMono-Regular")
        {
            if (sizeDelta500x500 == default)
                sizeDelta500x500 = new Vector2(500, 500);

            GameObject txtGO_internal = parentTransform.CreateChild(gOName, true);   //Get the TextGenerator? idk it's in the instantiated stuff but not in Text by default
            Text textComponent = txtGO_internal.AddComponent<Text>();

            var fontInternal = Resources.Load<UnityEngine.Font>(fontPath);
            if (fontInternal == null)
            {
                LogGoob.WriteLine($"ax15: {fontPath} is not a valid font path!  Picked random font instead.", MessageType.Warning);
                textComponent.RandomFont();
            }
            else
                textComponent.font = fontInternal;
            textComponent.rectTransform.sizeDelta = sizeDelta500x500;
            textComponent.fontSize = fontSize;

            return textComponent;
        }
        public static void GOToLocalTransformIn(this GameObject gameObject, GameObject destinationGO)
        {
            if (destinationGO == null)
                return;
            gameObject.transform.parent = destinationGO.transform;   //why nullref   //this is prombaly bad idea in retrospect
            gameObject.transform.localPosition = new Vector3(0, 0, 0);
            gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        }


        public static GameObject AssignCanvasGOPlusText(this GameObject currentGO, out Canvas canvasCpnt, out GameObject txtGO, out Text textCpnt)  //the compile-time constant issue is apparently a C# restriction according to Kazoo
        {
            Vector2 canvGO_rectSizeDelta = new Vector2(1400, 200);
            Vector2 txtGO_rectSizeDelta = new Vector2(500, 500);
            int textCpnt_fontSize = 40;

            GameObject canvGO = currentGO.GivesBirthTo("TestCanvasGO_SG", true);  //For some reason, spawns with a text component visible from the main GameObject, idk why
            canvasCpnt = canvGO.AddComponent<Canvas>();  //rectTransform seems to come prepackaged for some reason idfk
            canvasCpnt.worldCamera = Locator.GetPlayerCamera().mainCamera;  //Check when canvases are set, and you'll find these values as the only ones being set
            canvasCpnt.renderMode = RenderMode.WorldSpace;
            canvGO.GetComponent<RectTransform>().sizeDelta = canvGO_rectSizeDelta;

            txtGO = canvGO.GivesBirthTo("TestTextGO_SG", true);   //Get the TextGenerator? idk it's in the instantiated stuff but not in Text by default
            textCpnt = txtGO.AddComponent<Text>();
            textCpnt.fontSize = textCpnt_fontSize;
            txtGO.GetComponent<RectTransform>().sizeDelta = txtGO_rectSizeDelta; //size of the box ENCLOSING text; doesn't affect text size, just strangulates it

            txtGO.AddComponent<TypeEffectText>();

            return canvGO;


            //is this a whole text component in and of itself?
            //_sgpTxtGO_Child = _sgpGO_NOMCanvas.transform.InstantiateTextGO_FromPathForSomeReason("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "Children",
            // out _sgpTxt_Child, new Vector2(siblingAlignment + 900, 75), textSizeDelta, horizontalOverflow);
            //
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


        public static void RandomFont(this Text textCpntToRandomize, int internalSize_ThisDoesNothingIdk = 100)
        {
            var fontList = UnityEngine.Font.GetOSInstalledFontNames();
            int fontIndex = UnityEngine.Random.Range(0, (fontList.Count()));
            textCpntToRandomize.font = UnityEngine.Font.CreateDynamicFontFromOSFont(fontList[fontIndex].ToString(), internalSize_ThisDoesNothingIdk);  //idk what the size parameter does; i've set it to 1 and to 100 and there's no noticeable difference; maybe it's instantly getting overwritten by something?  idk
        }


        public static int AdjacentSibIndexIn(this int currentIndex, List<GameObject> listToCheck, int toDirection = 1)
        {
            var listLength = listToCheck.Count;
            if (listLength >= 1 && listLength >= currentIndex)
            {
                var internalIndex = currentIndex;
                internalIndex += toDirection;
                internalIndex = ((internalIndex > listLength - 1) ? 0 : internalIndex);              //0323_1519: Idiot says this will always wrap around the list using "modulo" and Corby says to use .Count since .Count() will return Linq which is "stinky"
                internalIndex = ((internalIndex < 0) ? listLength - 1 : internalIndex);
                return internalIndex;
            }
            throw new Exception("AdjacentIndexIn: listToCheck.Count was smaller than 1, or currentIndex was bigger than currentIndex");
        }


        public static GameObject AdjacentSiblingIn(this int currentIndex, List<GameObject> listToCheck, int toDirection = 1)
        {
            int internalIndex = currentIndex.AdjacentSibIndexIn(listToCheck, toDirection);
            GameObject foundObject = listToCheck[internalIndex];
            if (foundObject != null)
                return foundObject;
            throw new Exception($"foundObject was null!");
        }

        public static GameObject AdjacentSiblingOfGOIn(this GameObject currentGO, List<GameObject> listToCheck, int toDirection = 1)
        {
            if (!listToCheck.Contains(currentGO))
                throw new Exception($"No such GO exists in {listToCheck}, have you considered dying for your sins?");
            var currentIndex = currentGO.transform.GetSiblingIndex();
            var foundObject = currentIndex.AdjacentSiblingIn(listToCheck, toDirection);
            return foundObject;
        }

        public static List<GameObject> GetAllSiblings(this GameObject gameObject) //If you wanted to put this in another class, you'd get rid of the "this"
        {
            if (gameObject.transform.parent == null)  //
            {
                List<GameObject> soloList = new List<GameObject> { gameObject };
                return soloList;
            }
            var siblings = new List<GameObject>();
            foreach (Transform sister in gameObject.transform.parent)  //IF YOU'RE ALREADY AS HIGH AS YOU CAN GET, THERE'S NO WAY TO FIND SIBLINGS???????
                siblings.Add(sister.gameObject);
            return siblings;
        }
        public static List<GameObject> ListChildrenOrNull(this GameObject current) //thanks to Corby and Idiot 
        {
            if (current == null || current.transform == null)
                throw new Exception($"current GameObject {current} was null, or somehow missing a transform");
            if (current.transform.childCount <= 0)  //nullref'd //nullref'd again, computer line 420(nice)
            {
                LogGoob.WriteLine($"X.ListChildrenOrNull~260: {current} didn't have any children, returned null.");
                return null;
            }

            var children = new List<GameObject>();
            foreach (Transform child in current.transform)
                children.Add(child.gameObject);
            return children;
        }

        public static GameObject FindIndexedGOIn(this int gOIndex, List<GameObject> inList)  //_siblingsOfSelGO starts null  
        {
            if (inList == null || gOIndex < 0)
                throw new Exception($"Null: a gOIndex of {gOIndex} and a list of {inList}");
            if (gOIndex > inList.Count)
                throw new Exception("GetGOAtIndexIfPossible was called with a gOIndex greater than the inList.Count!");

            return inList[gOIndex];            //something is making empty _siblingsOfSelGO lists - like, not even containing _selecIndex.  where is it coming from 
        }

        //Corby says Linq is cleaner but less readable; Learn about arrays from this, but do not learn from the Linq
        public static GameObject[] GetAllChildrenButLinqAndArrayInstead(this GameObject parent) //the [] tell code that it will be an array.
        {
            return parent.transform.Cast<Transform>().Select(child => child.gameObject).ToArray();
        }

        /// <summary>
        /// Got this bad boy from Judah Gabriel Himango, stackoverflow, 2013, at https://stackoverflow.com/questions/907995/filter-a-string
        /// </summary>
        /// <param name="input">A punctuated, messy string!</param>
        /// <returns>ACleanUnpunctuatedSpacelessString</returns>
        public static string GetGoodString(string input)
        {
            var allowedChars =
               Enumerable.Range('0', 10).Concat(
               Enumerable.Range('A', 26)).Concat(
               Enumerable.Range('a', 26)).Concat(
               Enumerable.Range('-', 1));

            var goodChars = input.Where(c => allowedChars.Contains(c));
            return new string(goodChars.ToArray());
        }

        public static string GOToStringOrElse(GameObject candidate, string stringIfNull = "")
        {
            if (candidate == null)
                return stringIfNull;
            return $"{candidate}";
        }


        public static void TextFromAdjacentSiblingsIn(this GameObject ofGameObject, List<GameObject> inList, out string upperSibling, out string lowerSibling, string textIfNoSiblings = "")
        {
            GameObject upperSibGOInternal = null;
            GameObject lowerSibGOInternal = null;

            if (ofGameObject != null)
                if (ofGameObject.transform.parent.childCount > 1)
                {
                    upperSibGOInternal = ofGameObject.AdjacentSiblingOfGOIn(inList, 1);
                    upperSibGOInternal = ofGameObject.AdjacentSiblingOfGOIn(inList, -1);
                }
            upperSibling = GOToStringOrElse(upperSibGOInternal, textIfNoSiblings);
            lowerSibling = GOToStringOrElse(lowerSibGOInternal, textIfNoSiblings);
        }

        public static void StringAdjacentSiblingsElse(this int ofIndex, List<GameObject> inList, out string upperSibling, out string lowerSibling, string textIfNoSiblings = "")
        {
            GameObject upperSibGOInternal = null;
            GameObject lowerSibGOInternal = null;
            GameObject internalGOConvert = ofIndex.FindIndexedGOIn(inList);

            if (internalGOConvert != null)
            {
                upperSibGOInternal = ofIndex.AdjacentSiblingIn(inList, 1);
                lowerSibGOInternal = ofIndex.AdjacentSiblingIn(inList, -1);
            }
            upperSibling = GOToStringOrElse(upperSibGOInternal, textIfNoSiblings);
            lowerSibling = GOToStringOrElse(lowerSibGOInternal, textIfNoSiblings);
        }

    }

}

