using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public Canvas _sgPropCanvas;
        public GameObject _sgpCanvObj;
        public GameObject _sgPropScreen;
        public Text _sgpParentOfTarget;
        private bool updateHasBegun = false;
        private RectTransform _mainTextRecTra;
        public GameObject _sgPropGOSelf;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I.


        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 

        private void Awake()   //032123_1602: Everything except _sgOwnPropGroupject starts null when you first wake up.  this seems ill-advised unless it's a side-effect of my current bigger issue
        {
            SpawnAdditionalLasses();

            //_sgOwnPropGroupject = ScaleGun420Modbehavior.Instance._ //Might have to define it here.  How do I break the chains?
            this._sgPropCanvas.enabled = false; //031823_0614: doing this since TranslatorProp did it but it wasn't here yet //update: nope //031823_1524: Sudden unexpected nullref?
            this._sgPropGOSelf.SetActive(false);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) 
        }

        private void SpawnAdditionalLasses()
        {



            //copy vertical layout separately 

            _sgpCanvObj = Instantiate(GameObject.Find("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas"), _sgPropGOSelf.transform);

            _sgpCanvObj.transform.localEulerAngles = new Vector3(25f, 160f, 350f);
            _sgpCanvObj.transform.localPosition = new Vector3(0.15f, 1.75f, 0.05f);
            _sgpCanvObj.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
            _sgpCanvObj.SetActive(true);  //031823_0616: This is a definite "true" moment (don't change)
            _sgPropCanvas = base.transform.GetComponentInChildren<Canvas>(true);  //031823_0627: GETTING RID OF THE (true) MAYBE?   //031923_1831: never found out whether that would work because VS broke

            _mainTextRecTra = base.transform.GetComponentInChildren<RectTransform>(true); //031823_0523: swapped to before _sgpTextFieldMain gets defined, idk why
            _mainTextRecTra.pivot = new Vector2(1f, 0.5f);

            _sgpParentOfTarget = _sgpCanvObj.transform.GetChildComponentByName<Text>("PageNumberText").GetComponent<Text>();   //THIS IS ALL YOU NEED TO SPAWN NEW LASSES YOU DINGUS

            _sgpParentOfTarget.enabled = true;  //031823_0608: setting to false doesn't fix the thing, and just leaves it disabled.
            _sgpParentOfTarget.rectTransform.sizeDelta = new Vector2(1000, 35);

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
                TheLogGoober.WriteLine($"set updateHasBegun to {updateHasBegun} (true)");
            }
        }

        public void OnEquipTool()   //done & working  //032123_1550: forcing this method made the staff start working, meaning something in the ToolClass isn't enabling
        {
            base.enabled = true;  //just like translatorprop, 
            this._sgPropCanvas.enabled = true; //032123_1605: if putting this down here fixes it, i swear... //032123_1613: I was building to the wrong directory.  now i have it working, no bugs.  the world may never know
            _sgPropGOSelf.SetActive(true);  //032123_1535: not set to instance of an object? 
        }
        public void OnUnequipTool() //done & working
        {
            GetComponentInChildren<ScalegunToolClass>().ClearTerminal();
            base.enabled = false;
        }

        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            this._sgPropCanvas.enabled = false; //031823_0611: Enabled this code, didn't fix anything, but it's what the translator prop does.  //032123_1543: disabled this code again, and if it starts working again then I think the canvas is getting called early //reenabling
            _sgPropGOSelf.SetActive(false);
        }

        public void SubmitGOs(GameObject currentObject)
        {
            _sgpParentOfTarget.text = currentObject.transform.parent.ToString();
        }
        public void UpdateVertArray()
        { }
        

        //  vv  NO LONGER IN USE HERE  vv , INSTEAD CALLED DURING THE MAIN MODBEHAVIOR CLASS DURING GOSetup USING THE InstantiatePrefab EXTENSION; THIS IS JUST HERE FOR REFERENCE

    }
}
