using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


//032123_1546: Staff is starting disabled again but refuses to enable

namespace ScaleGun420   //031923_1832: CURRENTLY, B DOESN'T WORK ON THE FIRST EQUIP, ONLY WORKS AFTER SUBSEQUENT EQUIPS, NO IDEA WHY, GOOD LUCK FIGURING IT OUT
{
    public class ScalegunPropClass : MonoBehaviour
    {



        //public GameObject _sgPropScreen;
        private GameObject _sgPropPrimitive1;


        public Canvas _sgp_THCanvas;
        private Canvas _sgp_NOMCanvas;

        private Text _sgpTxt_Selection;
        private Text _sgpTxt_Parent;
        private Text _sgpTxt_SibAbove;
        private Text _sgpTxt_SibBelow;
        private Text _sgpTxt_Child;


        public GameObject _sgpGO_THCanvasOBSOLETE;
        public GameObject _sgpGO_NOMCanvasOBSOLETE; //This can be phased out eventually with enough position tweaking, but for now it's good reference
        private RectTransform _mainTextRecTraOBSOLETE;
        private GameObject _sgpTxtGO_ChildOBSOLETE;
        private GameObject _sgpTxtGO_SibBelowOBSOLETE;
        private GameObject _sgpTxtGO_SibAboveOBSOLETE;
        private GameObject _sgpTxtGO_ParentOBSOLETE;
        private GameObject _sgpTxtGO_SelectionOBSOLETE;

        private string _greatGObbyFilter = $"(UnityEngine.GameObject)";

        private bool updateHasBegun = false;

        private GameObject _sgPropGOSelf;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I  //That is because it is a PREFAB, preexisting gameobjects with specific classes ALREADY ASSIGNED TO THEM jesus christ
        //use base.transform instead

        //private ScalegunToolClass _propsToolRef;
        private SgComputer _computer;


        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 

        private void Awake()   //032123_1602: Everything except _sgOwnPropGroupject starts null when you first wake up.  this seems ill-advised unless it's a side-effect of my current bigger issue
        {
            LogGoob.WriteLine("ScalegunPropClass is woke, grabbing SgComputer...", OWML.Common.MessageType.Success);

            _sgPropGOSelf = base.transform.gameObject;
            //_propsToolRef = _sgPropGOSelf.GetComponent<ScalegunToolClass>();
            _computer = gameObject.GetComponent<SgComputer>();

            _sgpTxt_Parent = this.transform.GetChildComponentByName<Text>("SG_Parent");  //can probably make this an enumerator, then do a forEach.
            _sgpTxt_Selection = this.transform.GetChildComponentByName<Text>("SG_Selection");
            _sgpTxt_SibAbove = this.transform.GetChildComponentByName<Text>("SG_SiblingUp");
            _sgpTxt_SibBelow = this.transform.GetChildComponentByName<Text>("SG_SiblingDown");
            _sgpTxt_Child = this.transform.GetChildComponentByName<Text>("SG_Babens");


            _sgp_NOMCanvas = this.transform.GetChildComponentByName<Canvas>("SG_NOMCanvas");
            _sgp_THCanvas = this.transform.GetChildComponentByName<Canvas>("SG_THCanvas");
        }
        private void OldSpawnRoutines()
        {
            _sgpGO_THCanvasOBSOLETE = Instantiate(GameObject.Find("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas"), _sgPropGOSelf.transform);

            _sgpGO_THCanvasOBSOLETE.name = "ScaleGunCanvasHearth";

            _sgpGO_THCanvasOBSOLETE.transform.localEulerAngles = new Vector3(25f, 160f, 350f);
            _sgpGO_THCanvasOBSOLETE.transform.localPosition = new Vector3(0.15f, 1.75f, 0.05f);
            _sgpGO_THCanvasOBSOLETE.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
            _sgpGO_THCanvasOBSOLETE.SetActive(true);  //031823_0616: This is a definite "true" moment (don't change)
            //_sgpGO_THCanvas = base.transform.GetComponentInChildren<Canvas>(true);  //031823_0627: GETTING RID OF THE (true) MAYBE?   //031923_1831: never found out whether that would work because VS broke
            _sgp_THCanvas = base.transform.GetComponentInChildren<Canvas>(true);



            _mainTextRecTraOBSOLETE = base.transform.GetComponentInChildren<RectTransform>(true); //031823_0523: swapped to before _sgpTextFieldMain gets defined, idk why
            _mainTextRecTraOBSOLETE.pivot = new Vector2(1f, 0.5f);

            _sgpTxt_Selection = _sgpGO_THCanvasOBSOLETE.transform.GetChildComponentByName<Text>("TranslatorText").GetComponent<Text>();
            _sgpTxt_Selection.name = "SelectedObject";
            _sgpTxt_Selection.rectTransform.localPosition = new Vector2(1100, 260);
            _sgpTxt_Selection.rectTransform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            _sgpTxt_Selection.alignment = TextAnchor.MiddleCenter;
            var horizontalOverflow = HorizontalWrapMode.Overflow;
            var textSizeDelta = new Vector2(1400, 35);


            _sgpTxt_Parent = _sgpGO_THCanvasOBSOLETE.transform.GetChildComponentByName<Text>("PageNumberText").GetComponent<Text>();   //THIS IS ALL YOU NEED TO SPAWN NEW LASSES YOU DINGUS
            _sgpTxt_Parent.name = "ParentOfSelectedObject";
            _sgpTxt_Parent.rectTransform.localPosition = new Vector2(-1680, 245);
            _sgpTxt_Parent.rectTransform.sizeDelta = textSizeDelta;
            _sgpTxt_Parent.horizontalOverflow = horizontalOverflow;


            float siblingAlignment = -835;

            _sgpTxtGO_SibAboveOBSOLETE = _sgpGO_THCanvasOBSOLETE.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "TopSibling",
                out _sgpTxt_SibAbove, new Vector2(siblingAlignment, 140), textSizeDelta, horizontalOverflow);

            _sgpTxtGO_SibBelowOBSOLETE = _sgpGO_THCanvasOBSOLETE.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "BottomSibling",
                out _sgpTxt_SibBelow, new Vector2(siblingAlignment, 0), textSizeDelta, horizontalOverflow);


            _sgpGO_NOMCanvasOBSOLETE = _sgPropGOSelf.GivesBirthTo("ScalegunCanvasNomai", true, new Vector3(0, 1.7f, 0.15f), new Vector3(45, 180, 0), 0.003f);  //For some reason, spawns with a text component visible from the main GameObject, idk why
            _sgp_NOMCanvas = _sgpGO_NOMCanvasOBSOLETE.AddComponent<Canvas>();  //rectTransform seems to come prepackaged for some reason idfk
            _sgp_NOMCanvas.worldCamera = Locator.GetPlayerCamera().mainCamera;  //Check when canvases are set, and you'll find these values as the only ones being set
            _sgp_NOMCanvas.renderMode = RenderMode.WorldSpace;

            _sgpTxtGO_ChildOBSOLETE = _sgpGO_NOMCanvasOBSOLETE.GivesBirthTo("Children", true);   //Get the TextGenerator? idk it's in the instantiated stuff but not in Text by default
            _sgpTxt_Child = _sgpTxtGO_ChildOBSOLETE.AddComponent<Text>();
            RandomizeFontForSomeReason();
            _sgpTxtGO_ChildOBSOLETE.AddComponent<TypeEffectText>(); //is this a whole text component in and of itself?

            //_sgpTxtGO_Child = _sgpGO_NOMCanvas.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "Children",
            // out _sgpTxt_Child, new Vector2(siblingAlignment + 900, 75), textSizeDelta, horizontalOverflow);

            //
            _sgpTxt_Parent.enabled = true;  //031823_0608: setting to false doesn't fix the thing, and just leaves it disabled. //032623_1921: idk why this is still here but I'll leave it for now.

            //_sgOwnPropGroupject = ScaleGun420Modbehavior.Instance._ //Might have to define it here.  How do I break the chains?
            this._sgp_NOMCanvas.enabled = false;
            this._sgp_THCanvas.enabled = false; //031823_0614: doing this since TranslatorProp did it but it wasn't here yet //update: nope //031823_1524: Sudden unexpected nullref?
            this._sgPropGOSelf.SetActive(false);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) }
        }
        private void RandomizeFontForSomeReason()
        {
            var fontList = Font.GetOSInstalledFontNames();
            int fontIndex = UnityEngine.Random.Range(0, (fontList.Count()));
            _sgpTxt_Child.font = UnityEngine.Font.CreateDynamicFontFromOSFont(fontList[fontIndex].ToString(), 100);  //idk what the size parameter does; i've set it to 1 and to 100 and there's no noticeable difference; maybe it's instantly getting overwritten by something?  idk
        }

        private void Start()
        {
            base.enabled = false;
        }  // Just like TranslatorProp without all the BS


        private void Update()  //If update isn't running after the first equip, what else is broken?  //032123_1648: Confirmed update isn't running on first equip.
        {
            if (updateHasBegun == false)
            {
                updateHasBegun = true;
            }
        }

        public void OnEquipTool()   //done & working  //032123_1550: forcing this method made the staff start working, meaning something in the ToolClass isn't enabling
        {
            base.enabled = true;  //just like translatorprop, 
            this._sgp_NOMCanvas.enabled = true;
            this._sgp_THCanvas.enabled = true; //032123_1605: if putting this down here fixes it, i swear... //032123_1613: I was building to the wrong directory.  now i have it working, no bugs.  the world may never know
            _sgPropGOSelf.SetActive(true);  //032123_1535: not set to instance of an object? 
            RandomizeFontForSomeReason();
        }
        public void OnUnequipTool() //done & working
        {
            base.enabled = false;
        }



        //MAYBE MAKE ENUMERATOR FOR ALL HIERARCHY NAVIGATION DIRECTIONS, UNIFY IT?


        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            this._sgp_NOMCanvas.enabled = false;
            this._sgp_THCanvas.enabled = false; //031823_0611: Enabled this code, didn't fix anything, but it's what the translator prop does.  //032123_1543: disabled this code again, and if it starts working again then I think the canvas is getting called early //reenabling
            _sgPropGOSelf.SetActive(false);
        }

        private bool ShouldEditText(string stringToCheck)
        { return (stringToCheck != "SKIP" && stringToCheck != "SKIP(UnityEngine.GameObject)"); }

        public void UpdateScreenTextV2(string parentOrSKIP, string sibAboveOrSKIP, string sibBelowOrSKIP, string childOrSKIP, string currentSelFieldOverride = $"GetCurrentSelectionOugh_ax15")
        {
            //once I figure out how the TypeEffectText thing works, have separate thing like "ax15_ROLLTEXT_ax15" to evoke stuff like I do with "SKIP"

            if (ShouldEditText(parentOrSKIP))
            { _sgpTxt_Parent.text = parentOrSKIP; }
            if (ShouldEditText(sibAboveOrSKIP))
            { _sgpTxt_SibAbove.text = sibAboveOrSKIP; }
            if (ShouldEditText(sibBelowOrSKIP))
            { _sgpTxt_SibBelow.text = sibBelowOrSKIP; }

            if (ShouldEditText(currentSelFieldOverride))
            {
                if (currentSelFieldOverride == $"GetCurrentSelectionOugh_ax15")
                {
                    GameObject currentComputerSelection = _computer.SelectedGOAtIndex();  //don't forget the parentheses or the formatting will be arbitrarily different, awesome
                    if (currentComputerSelection != null)  //changed from _selectedObject to GetCurrentSelection, idk what the consequences of this will be
                    { _sgpTxt_Selection.text = $"{currentComputerSelection}"; }
                }
                else
                { _sgpTxt_Selection.text = currentSelFieldOverride; }
            }

            //TURN THIS INTO SHIP-DAMAGE-INDICATOR-TYPE OBJECT, USE SCROLLTEXT, ETC
            if (ShouldEditText(childOrSKIP))
            { _sgpTxt_Child.text = childOrSKIP; }

            foreach (Text textLass in _sgPropGOSelf.GetComponentsInChildren<Text>())  //this can probably run elsewhere and be more efficient idk //it also doesn't work
            { textLass.text = TrimmedText(textLass.text); }
        }
        private string TrimmedText(string victim)              //why doesn't it work
        {
            victim = victim.Replace(_greatGObbyFilter, ""); //strOne = "Hello" now
            return victim;   //trying to return victim, idk if this'll work
        }




        //  vv  NO LONGER IN USE HERE  vv , INSTEAD CALLED DURING THE MAIN MODBEHAVIOR CLASS DURING GOSetup USING THE InstantiatePrefab EXTENSION; THIS IS JUST HERE FOR REFERENCE

    }
}
