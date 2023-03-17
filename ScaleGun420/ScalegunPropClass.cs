﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public class ScalegunPropClass : MonoBehaviour
    {
        public Canvas _sgPropCanvas;
        public GameObject _sgPropStaff;
        public GameObject _sgPropScreen;
        public GameObject _sgOwnPropGroupject;  //TranslatorProp never had to GetComponent() or whatever to define its internal _translatorProp Gameobject, so presumably, neither do I.

        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 
        private void Awake()
        {   
            RenderNomaiStaff();
            //this._sgPropStaff.SetActive(false);  //UNNECESSARY I THINK, STAFF IS ALREADY PART OF _sgOwnPropGroupject
            //this._sgPropCanvas.enabled = false;
            this._sgOwnPropGroupject.SetActive(false);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) 
        }
        private void Start()
        { base.enabled = false; }  // Just like TranslatorProp without all the BS


        public void OnEquipTool()   //done
        {
            base.enabled = true;
            //this._sgPropCanvas.enabled = true;
            _sgPropStaff.SetActive(true);
        }
        public void OnUnequipTool() //done
        { base.enabled = false; }

        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            //this._sgPropCanvas.enabled = false;
            _sgOwnPropGroupject.SetActive(false);
        }


        private void RenderNomaiStaff()
        {
            LoadStaff();  //31623_0507: two lines down you'll notice the .Find() has an overload telling it which 
            _sgPropStaff = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"), _sgOwnPropGroupject.transform);

            _sgPropStaff.transform.localPosition = new Vector3(0.5496f, -1.11f, -0.119f);
            _sgPropStaff.transform.localEulerAngles = new Vector3(343.8753f, 200.2473f, 345.2718f);
            var streamingRenderMeshHandle = _sgPropStaff.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadStaff;
            void LoadStaff() { StreamingManager.LoadStreamingAssets("brittlehollow/meshes/props"); }
        }
    }
}