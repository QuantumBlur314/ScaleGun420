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


//resolved //032123_1546: Staff is starting disabled again but refuses to enable

namespace ScaleGun420   //031923_1832: CURRENTLY, B DOESN'T WORK ON THE FIRST EQUIP, ONLY WORKS AFTER SUBSEQUENT EQUIPS, NO IDEA WHY, GOOD LUCK FIGURING IT OUT
{
    /// <summary>
    /// MISSIONS:
    /// A.) using Transform.SetParent(Transform theTransform, bool worldPositionStays = false) to pop the sibling text husks over to the child canvas 
    /// </summary>
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

        private string _greatGObbyFilter = $"(UnityEngine.GameObject)";

        private bool updateHasBegun = false;

        private GameObject _sgPropGOSelf;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I  //That is because it is a PREFAB, preexisting gameobjects with specific classes ALREADY ASSIGNED TO THEM jesus christ
        //use base.transform instead

        //private ScalegunToolClass _propsToolRef;
        private SgNavComputer _computer;



        public GameObject _testHuskCanvas;
        public Canvas _testCpntCanvas;
        public GameObject _testHuskText;
        public Text _testCpntText;
        public RectTransform _testCanvRectTransform;


        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 

        private void Awake()   //032123_1602: Everything except _sgOwnPropGroupject starts null when you first wake up.  this seems ill-advised unless it's a side-effect of my current bigger issue
        {
            LogGoob.WriteLine("ScalegunPropClass is woke, grabbing SgNavComputer...", OWML.Common.MessageType.Success);


            _sgPropGOSelf = GameObject.Find("ScalegunGroup");
            //_sgPropGOSelf = base.transform.gameObject;
            //_propsToolRef = _sgPropGOSelf.GetComponent<ScalegunToolClass>();
            _computer = gameObject.GetComponent<SgNavComputer>();

            _sgpTxt_Parent = this.transform.GetChildComponentByName<Text>("SG_Parent");  //can probably make this an enumerator, then do a forEach.
            if (_sgpTxt_Parent = null)
            { LogGoob.WriteLine("_sgpTxt_Parent was null", OWML.Common.MessageType.Warning); }

            _sgp_NOMCanvas = this.transform.GetChildComponentByName<Canvas>("SG_NOMCanvas");
            _sgp_THCanvas = this.transform.GetChildComponentByName<Canvas>("SG_THCanvas");


            //PARENT AND SIBLING FIELDS (the original two THCanvas fellas, directly cloned from the translator) ARE BOTH NULL; presumably hierarchy differences  //NVM THEY WEREN'T GETTING RENAMED, SPAWNER ISSUE 
            _sgpTxt_Parent = _sgp_THCanvas.transform.GetChildComponentByName<Text>("SG_Parent"); //forgor (:-#
            _sgpTxt_Selection = _sgp_THCanvas.transform.GetChildComponentByName<Text>("SG_Selection");

            _sgpTxt_SibAbove = _sgp_THCanvas.transform.GetChildComponentByName<Text>("SG_SiblingUp");
            _sgpTxt_SibBelow = _sgp_THCanvas.transform.GetChildComponentByName<Text>("SG_SiblingDown");
            _sgpTxt_Child = _sgp_NOMCanvas.transform.GetChildComponentByName<Text>("SG_Babens");

            LogGoob.WriteLine($"ScalegunPropClass reports SgNavComputer _computer is {_computer} attached to {_computer.transform.gameObject}");

            _sgPropGOSelf.SetActive(false);
        }
        private void Start()
        {
            base.enabled = false;
        }  // Just like TranslatorProp without all the BS


        private void RandomizeFontForSomeReason(Text textCpntToRandomize)
        {
            var fontList = Font.GetOSInstalledFontNames();
            int fontIndex = UnityEngine.Random.Range(0, (fontList.Count()));
            textCpntToRandomize.font = UnityEngine.Font.CreateDynamicFontFromOSFont(fontList[fontIndex].ToString(), 100);  //idk what the size parameter does; i've set it to 1 and to 100 and there's no noticeable difference; maybe it's instantly getting overwritten by something?  idk
        }



        private void Update()  //If update isn't running after the first equip, what else is broken?  //032123_1648: Confirmed update isn't running on first equip.
        {
            if (updateHasBegun == false)
                updateHasBegun = true;
        }

        public void OnEquipTool()   //done & working  //032123_1550: forcing this method made the staff start working, meaning something in the ToolClass isn't enabling
        {
            base.enabled = true;  //just like translatorprop, 
            this._sgp_NOMCanvas.enabled = true;
            this._sgp_THCanvas.enabled = true; //032123_1605: if putting this down here fixes it, i swear... //032123_1613: I was building to the wrong directory.  now i have it working, no bugs.  the world may never know
            _sgPropGOSelf.SetActive(true);  //032123_1535: not set to instance of an object? 
            _sgpTxt_Child.RandomFont();
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
        { return (stringToCheck != "SKIP" && stringToCheck != "SKIP (UnityEngine.GameObject)"); }

        private void FetchComponents()
        { }
        public void RefreshScreen(string parentOrSKIP, string sibAboveOrSKIP, string sibBelowOrSKIP, string childOrSKIP, string currentSelFieldOverride = $"ax15_computer._selectedObjectPublic_ax15")
        {
            //once I figure out how the TypeEffectText thing works, have separate thing like "ax15_ROLLTEXT_ax15" to evoke stuff like I do with "SKIP"

            if (ShouldEditText(parentOrSKIP))
            { _sgpTxt_Parent.text = parentOrSKIP; }  //why is this nullref'ing?  //This is just the first text object that UpdateScreenTextVTWO tries to edit, any nullrefs could be the result of 
            if (ShouldEditText(sibAboveOrSKIP))
            { _sgpTxt_SibAbove.text = sibAboveOrSKIP; }
            if (ShouldEditText(sibBelowOrSKIP))
            { _sgpTxt_SibBelow.text = sibBelowOrSKIP; }

            if (ShouldEditText(currentSelFieldOverride))
            {
                if (currentSelFieldOverride == $"ax15_computer._selectedObjectPublic_ax15")
                {
                    GameObject currentComputerSelection = _computer._selectedGOPublic;  //don't forget the parentheses or the formatting will be arbitrarily different, awesome
                    if (currentComputerSelection != null)  //changed from _selectedObject to GetCurrentSelection, idk what the consequences of this will be
                    { _sgpTxt_Selection.text = $"{currentComputerSelection}"; }
                }
                else
                { _sgpTxt_Selection.text = currentSelFieldOverride; }
            }

            //TURN THIS INTO SHIP-DAMAGE-INDICATOR-TYPE OBJECT, USE SCROLLTEXT, ETC
            //oh you sweet summer child
            if (ShouldEditText(childOrSKIP))
            { _sgpTxt_Child.text = childOrSKIP; }

            foreach (Text textLass in _sgPropGOSelf.GetComponentsInChildren<Text>())  //this can probably run elsewhere and be more efficient idk //it also doesn't work
            { textLass.text = TrimmedText(textLass.text); }

            if (_testCpntText != null)
                _testCpntText.text = childOrSKIP;
        }
        private string TrimmedText(string victim)              //why doesn't it work
        {
            victim = victim.Replace(_greatGObbyFilter, ""); //strOne = "Hello" now
            return victim;   //trying to return victim, idk if this'll work
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="huskCanvasLocPos"></param>
        /// 
        /// <param name="huskCanvScaleMult"></param>
        /// Scale of the GO housing the Canvas component.  To make contained text not-blurry, set the GO's scale to a thousandth or so, then scale up any contained textHusks
        /// <param name="rectCanvHoriz"></param>
        /// <param name="rectCanvVertic"></param>
        /// Horizontal and Vertical scale of the CanvasHusk's RectTransform.sizeDelta (which it only acquires when the canvas component is added i think
        public void SpawnTestCanvas(string gONameOtherThanStaff = null)
        {
            GameObject gOAttachTarget = _sgPropGOSelf;
            if (gONameOtherThanStaff != null && gONameOtherThanStaff != "")
            {
                GameObject altAttachTarget = GameObject.Find(gONameOtherThanStaff);
                if (altAttachTarget == null)
                    LogGoob.WriteLine($"Es gibt nicht ein GameObject namte {gONameOtherThanStaff}!", OWML.Common.MessageType.Warning);
                else gOAttachTarget = altAttachTarget;
            }

            string canvName = "CanvasGO_Test_SG";
            Vector2 canvRTSizeDelta = new Vector2(1400, 200);
            float goCanvScale = 0.0004f;
            Vector3 goCanvLocalPosition = new Vector3(0, 1.7f, 0.15f);
            Vector3 goCanvLocalEulers = new Vector3(45, 180, 0);

            _testCpntCanvas = gOAttachTarget.transform.AddCanvasGO(
                canvName, canvRTSizeDelta, goCanvScale, goCanvLocalPosition, goCanvLocalEulers);



            string nameText = "TextGO_Test_SG";
            Vector2 txt_rectSizeDelta = new Vector2(500, 500);
            int fontSize = 40;
            string spaceFontName = "fonts/english - latin/SpaceMono-Regular";

            _testCpntText = _testCpntCanvas.transform.AddTextGO(
                nameText, txt_rectSizeDelta, fontSize, spaceFontName);
        }
    }
}
