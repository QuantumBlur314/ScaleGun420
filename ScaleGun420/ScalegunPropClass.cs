using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static ScaleGun420.ScalegunToolClass;

//032123_1546: Staff is starting disabled again but refuses to enable

namespace ScaleGun420   //031923_1832: CURRENTLY, B DOESN'T WORK ON THE FIRST EQUIP, ONLY WORKS AFTER SUBSEQUENT EQUIPS, NO IDEA WHY, GOOD LUCK FIGURING IT OUT
{
    public class ScalegunPropClass : MonoBehaviour
    {
        public Canvas _sgp_THCanvas;
        private Canvas _sgp_NOMCanvas;
        public GameObject _sgpGO_THCanvas;
        public GameObject _sgPropScreen;
        private GameObject _sgPropPrimitive1;
        

        private GameObject _sgpTxtGO_Selection;
        private Text _sgpTxt_Selection;

        private GameObject _sgpTxtGO_Parent;
        private Text _sgpTxt_Parent;

        public GameObject _sgpTxtGO_SibAbove;
        private Text _sgpTxt_SibAbove;

        public GameObject _sgpTxtGO_SibBelow;
        private Text _sgpTxt_SibBelow;

        private GameObject _sgpTxtGO_Child;
        private Text _sgpTxt_Child;




        private bool updateHasBegun = false;
        private bool _prevSelectionToField; //whether to grab ScalegunToolClass._previousSelection to fill a child/parent field, rather than having to dig again; may work better as method, idk
        private RectTransform _mainTextRecTra;
        public GameObject _sgPropGOSelf;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I.


        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 

        private void Awake()   //032123_1602: Everything except _sgOwnPropGroupject starts null when you first wake up.  this seems ill-advised unless it's a side-effect of my current bigger issue
        {

            _sgpGO_THCanvas = Instantiate(GameObject.Find("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas"), _sgPropGOSelf.transform);

            _sgpGO_THCanvas.name = "ScaleGunCanvas";
            //_sgpGO_THCanvas = new GameObject("Scalegun_THCanvas");
            //_sgp_THCanvas = _sgpGO_THCanvas.AddComponent<Canvas>();


            _sgpGO_THCanvas.transform.localEulerAngles = new Vector3(25f, 160f, 350f);
            _sgpGO_THCanvas.transform.localPosition = new Vector3(0.15f, 1.75f, 0.05f);
            _sgpGO_THCanvas.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
            _sgpGO_THCanvas.SetActive(true);  //031823_0616: This is a definite "true" moment (don't change)
            //_sgpGO_THCanvas = base.transform.GetComponentInChildren<Canvas>(true);  //031823_0627: GETTING RID OF THE (true) MAYBE?   //031923_1831: never found out whether that would work because VS broke
            _sgp_THCanvas = base.transform.GetComponentInChildren<Canvas>(true);

            _mainTextRecTra = base.transform.GetComponentInChildren<RectTransform>(true); //031823_0523: swapped to before _sgpTextFieldMain gets defined, idk why
            _mainTextRecTra.pivot = new Vector2(1f, 0.5f);

            _sgpTxt_Selection = _sgpGO_THCanvas.transform.GetChildComponentByName<Text>("TranslatorText").GetComponent<Text>();
            _sgpTxt_Selection.name = "SelectedObject";
            _sgpTxt_Selection.rectTransform.localPosition = new Vector2(1100, 260);
            _sgpTxt_Selection.rectTransform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            _sgpTxt_Selection.alignment = TextAnchor.MiddleCenter;
            var horizontalOverflow = HorizontalWrapMode.Overflow;
            var textSizeDelta = new Vector2(1400, 35);


            _sgpTxt_Parent = _sgpGO_THCanvas.transform.GetChildComponentByName<Text>("PageNumberText").GetComponent<Text>();   //THIS IS ALL YOU NEED TO SPAWN NEW LASSES YOU DINGUS
            _sgpTxt_Parent.name = "ParentOfSelectedObject";
            _sgpTxt_Parent.rectTransform.localPosition = new Vector2(-1680, 245);
            _sgpTxt_Parent.rectTransform.sizeDelta = textSizeDelta;
            _sgpTxt_Parent.horizontalOverflow = horizontalOverflow;


            float siblingAlignment = -835;

            _sgpTxtGO_SibAbove = _sgpGO_THCanvas.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "TopSibling",
                out _sgpTxt_SibAbove, new Vector2(siblingAlignment, 140), textSizeDelta, horizontalOverflow);

            _sgpTxtGO_SibBelow = _sgpGO_THCanvas.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "BottomSibling",
                out _sgpTxt_SibBelow, new Vector2(siblingAlignment, 0), textSizeDelta, horizontalOverflow);

            _sgpTxtGO_Child = _sgpGO_THCanvas.transform.InstantiateTextObj("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "Children",
                out _sgpTxt_Child, new Vector2(siblingAlignment + 900, 75), textSizeDelta, horizontalOverflow);

            _sgpTxt_Parent.enabled = true;  //031823_0608: setting to false doesn't fix the thing, and just leaves it disabled. //032623_1921: idk why this is still here but I'll leave it for now.

            //_sgOwnPropGroupject = ScaleGun420Modbehavior.Instance._ //Might have to define it here.  How do I break the chains?
            this._sgp_NOMCanvas.enabled = false;
            this._sgp_THCanvas.enabled = false; //031823_0614: doing this since TranslatorProp did it but it wasn't here yet //update: nope //031823_1524: Sudden unexpected nullref?
            this._sgPropGOSelf.SetActive(false);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) 
        }



        public void SpawnAdditionalLasses()
        {

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
                LogGoob.WriteLine($"set updateHasBegun to {updateHasBegun} (true)");
            }
        }

        public void OnEquipTool()   //done & working  //032123_1550: forcing this method made the staff start working, meaning something in the ToolClass isn't enabling
        {
            base.enabled = true;  //just like translatorprop, 
            this._sgp_THCanvas.enabled = true; //032123_1605: if putting this down here fixes it, i swear... //032123_1613: I was building to the wrong directory.  now i have it working, no bugs.  the world may never know
            _sgPropGOSelf.SetActive(true);  //032123_1535: not set to instance of an object? 
        }
        public void OnUnequipTool() //done & working
        {
            GetComponentInChildren<ScalegunToolClass>().ClearTerminal();
            base.enabled = false;
        }
        public void OnToParentInit()   //maybe if user's scrolling through multiple parents, wait a bit before getting adjacent siblings in case they scroll again.
        {
            if (_selectedObject.transform.parent.gameObject != null)
            { _sgpTxt_Parent.text = $"{_selectedObject.transform.parent.gameObject}"; }
            _sgpTxt_SibAbove.text = $"waiting 4 scrolling 2 stop";
            _sgpTxt_Selection.text = $"{_selectedObject}";
            _sgpTxt_SibBelow.text = $"waiting 4 scrolling 2 stop";
            LogGoob.WriteLine($"{_previousSelection} is _previousSelection, and will now be set to the currently-null _sgpTxt_Child.text, which is a component of {_sgpTxtGO_Child}, and nothing will nullref ::)"); //THE TEXT IS NULL, CONFIRMED
            _sgpTxt_Child.text = $"{_previousSelection}";
        }

        public void OnToChilds()
        { }

        //MAYBE MAKE ENUMERATOR FOR ALL HIERARCHY NAVIGATION DIRECTIONS, UNIFY IT?
        public void OnDownSiblings()
        {
            var currentIndex = _selectedObject.transform.GetSiblingIndex();

            _sgpTxt_SibAbove.text = $"{_previousSelection},prevsel";
            _sgpTxt_Selection.text = $"{_selectedObject},{_selObjIndex}";
            _sgpTxt_SibBelow.text = $"{GetSiblingAt(-1)}, {GetSiblingAt(-1).transform.GetSiblingIndex()}";
        }

        public void OnUpSiblings()
        {
            var currentIndex = _selectedObject.transform.GetSiblingIndex();

            _sgpTxt_SibAbove.text = $"{GetSiblingAt(1)}, {GetSiblingAt(1).transform.GetSiblingIndex()}";
            _sgpTxt_Selection.text = $"{_selectedObject}, {_selObjIndex}";
            _sgpTxt_SibBelow.text = $"{_previousSelection}, prevsel";
        }

        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            this._sgp_THCanvas.enabled = false; //031823_0611: Enabled this code, didn't fix anything, but it's what the translator prop does.  //032123_1543: disabled this code again, and if it starts working again then I think the canvas is getting called early //reenabling
            _sgPropGOSelf.SetActive(false);
        }

        public void GetAdjacentSibling(int increment = 1)
        {

            if (_selGO_Siblings.Count <= 1) //Update already checks this but idk
            { return; }


            var selectedObjectIndex = _selectedObject.transform.GetSiblingIndex();   //how do i account for the list changing without having to rerun GetSiblings?  idk

            var myItem = _selGO_Siblings[(increment + 1) % _selGO_Siblings.Count];  //0323_1519: Idiot says this will always wrap around the list using "modulo" and Corby says to use .Count since .Count() will return Linq which is "stinky"
            _selectedObject = myItem;
        }
        public void UpdateScreenText()
        {
            if (_selectedObject == null)
            {
                foreach (Text textobject in _sgpGO_THCanvas.GetComponentsInChildren<Text>())
                { textobject.text = "_selectedObject IS NULL"; }
            } //this is wack
            else
            {
                _sgpTxt_SibAbove.text = $"{GetSiblingAt(1)}, {GetSiblingAt(1).transform.GetSiblingIndex()}";
                _sgpTxt_Selection.text = _selectedObject.ToString();
                _sgpTxt_SibBelow.text = $"{GetSiblingAt(-1)}, {GetSiblingAt(-1).transform.GetSiblingIndex()}";
                _sgpTxt_Parent.text = _selectedObject.transform.parent.ToString();  //this will be redundant once the Prop.OnScroll methods are finished
            }
        }



        //  vv  NO LONGER IN USE HERE  vv , INSTEAD CALLED DURING THE MAIN MODBEHAVIOR CLASS DURING GOSetup USING THE InstantiatePrefab EXTENSION; THIS IS JUST HERE FOR REFERENCE

    }
}
