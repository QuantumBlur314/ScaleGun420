using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


namespace ScaleGun420
{
    public class ScalegunPropClass : MonoBehaviour
    {
        public Canvas _sgPropCanvas;
        public GameObject _sgpCanvObj;
        public GameObject _sgPropStaff;
        public GameObject _sgPropScreen;
        public Text _sgpTextFieldMain;
        private RectTransform _mainTextRecTra;
        public GameObject _sgOwnPropGroupject;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I.

        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 

        private void Awake()
        {
            SpawnAdditionalLasses();

            this._sgOwnPropGroupject.SetActive(false);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) 
        }

        private void SpawnAdditionalLasses()
        {
            _sgpCanvObj = Instantiate(GameObject.Find("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas"), _sgOwnPropGroupject.transform);
            
            _sgpCanvObj.transform.localEulerAngles = new Vector3(45f, 180f, 0f);
            _sgpCanvObj.transform.localPosition = new Vector3(0.4f, 1.85f, 0.05f);
            _sgpCanvObj.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
            _sgPropCanvas = base.transform.GetComponentInChildren<Canvas>(true);

            _sgpTextFieldMain = _sgpCanvObj.transform.GetChildComponentByName<Text>("TranslatorText").GetComponent<Text>();   //THIS IS ALL YOU NEED TO SPAWN NEW LASSES YOU DINGUS

            _mainTextRecTra = base.transform.GetComponentInChildren<RectTransform>(true);
        }

        private void Start()
        { base.enabled = false; }  // Just like TranslatorProp without all the BS


        private void Update()
        { EyesDrillHoles(); }

        public void EyesDrillHoles()          //GameObjects have a SetActive method, the menu uses this, maybe it's single-target?  maybe I don't have to use my own thingus?
        {
            if (ScaleGun420Modbehavior.Instance.BigBubbon && Locator.GetPlayerCamera() != null && ScaleGun420Modbehavior.Instance._vanillaSwapper.IsInToolMode(ScaleGun420Modbehavior.Instance.SGToolmode))
            {
                Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

                Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
                var retrievedRootObject = hit.collider.transform.GetPath();
                _sgpTextFieldMain.text = $"{retrievedRootObject}";
            }
        }

        public void OnEquipTool()   //done
        {
            base.enabled = true;
            _sgpTextFieldMain.text = "AAAAAUAGH AUGH UGH AUGH AAAAHHHHHHHHHHH AHGH AGH HGHAH H GHERUHEOREOFUEAVOIEJ DAIFOREHNOESRGVEFHGS UOGVH OBSUI OIUHJ ESOE GCOIRE OUIESR?UMCOSERIOSMOVSER?RMJ";

            this._sgPropCanvas.enabled = true;
            _sgOwnPropGroupject.SetActive(true);  //reference

        }
        public void OnUnequipTool() //done
        { base.enabled = false; }

        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            //this._sgPropCanvas.enabled = false;
            _sgOwnPropGroupject.SetActive(false);
        }


        private void RenderNomaiStaff()
        {
            LoadStaff();  //31623_0507: two lines down you'll notice the .Find() has an overload telling it what to be the child of
            _sgPropStaff = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"), _sgOwnPropGroupject.transform);

            _sgPropStaff.transform.localPosition = new Vector3(0.5496f, -1.11f, -0.119f);
            _sgPropStaff.transform.localEulerAngles = new Vector3(343.8753f, 200.2473f, 345.2718f);
            var streamingRenderMeshHandle = _sgPropStaff.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadStaff;   //031623_2047: I think the Loadstaff might be getting called repeatedly or something, idk, performance is garbage when equipped
            void LoadStaff() { StreamingManager.LoadStreamingAssets("brittlehollow/meshes/props"); }
        }

    }
}
