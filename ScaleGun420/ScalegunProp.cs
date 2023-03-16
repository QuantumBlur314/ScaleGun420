using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public class ScalegunProp : MonoBehaviour
    {
        public Canvas _sgPropCanvas;
        public GameObject _sgPropStaff;
        public GameObject _sgPropScreen;
        public GameObject _sgPropGO;

        //NomaiTranslatorProp only disables TranslatorGroup (the dingus housing all canvas, prop model, etc) near the end of NomaiTranslatorProp's Awake 
        private void Awake()
        {
            _sgPropGO = ScaleGun420Modbehavior.Instance._theGunToolClass._sgPropGO;  //error //It's grabbing _theGunToolClass._sgPropGO on awake, before _sgPropGO gets its PlayerTool assigned
            //_sgPropCanvas = _sgPropGO.SpawnChildCanvasAtParent();
            RenderNomaiStaff();
            this._sgPropStaff.SetActive(false);
            //this._sgPropCanvas.enabled = false;
        }
        private void Start()
        { base.enabled = false; }


        public void OnEquipTool()   //done
        {
            base.enabled = true;
            //this._sgPropCanvas.enabled = true;
            this._sgPropStaff.SetActive(true);
        }
        public void OnUnequipTool() //done
        { base.enabled = false; }

        public void OnFinishUnequipAnimation()  //called by Tool's OnDisable, just like bart just like bart just like bart just like bart just like bart just like bart jut like bart just like bart just lik ebart just line bart just koll bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart just like bart
        {
            //this._sgPropCanvas.enabled = false;
            this._sgPropStaff.SetActive(false);
        }


        private void RenderNomaiStaff()
        {
            LoadStaff();
            _sgPropStaff = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"), ScaleGun420Modbehavior.Instance._sgToolGameobject.transform);

            _sgPropStaff.transform.localPosition = new Vector3(0.5496f, -1.11f, -0.119f);
            _sgPropStaff.transform.localEulerAngles = new Vector3(343.8753f, 200.2473f, 345.2718f);
            var streamingRenderMeshHandle = _sgPropStaff.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadStaff;
            void LoadStaff() { StreamingManager.LoadStreamingAssets("brittlehollow/meshes/props"); }
        }
    }
}
