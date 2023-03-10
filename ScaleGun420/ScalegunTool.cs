using HarmonyLib;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{

    public class ScalegunTool : PlayerTool     //Remember to account for times when the game auto-deploys the Scout Launcher //Edit2: NHExamples makes it clear that it's probably easier/default
    {
        private Transform _scalegunToolTransform; //used by Awake
        private GameObject _staffObject;
        public GameObject _staffProp;
        private bool _justEquipped;
        public bool _alwaysVisible;
        private Canvas _canvas;


        //help


        //Do I have to define PlayerTool._stowTransform and _holdTransform?  
        private void Awake()
        {
            RenderNomaiStaff();
          StealOtherToolTransforms();
        }


        public override void Start()
        {
            base.Start();
            base.enabled = true;
        }
        public void RenderNomaiStaff()
        {
            LoadStaff();
            _staffProp = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"),ScaleGun420.Instance._vesselThroughWhichIExertMyWill.transform);
            _staffProp.transform.localPosition = new Vector3(0.5496f, -1.11f, -0.119f);
            _staffProp.transform.localEulerAngles = new Vector3(343.8753f, 200.2473f, 345.2718f);
            var streamingRenderMeshHandle = _staffProp.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadStaff;
            void LoadStaff() { StreamingManager.LoadStreamingAssets("brittlehollow/meshes/props"); }
            ScaleGun420.Instance.ModHelper.Console.WriteLine("THE STICK HAS BEEN LOADED, GET HUNTING");
        }
        private void StealOtherToolTransforms()
      {              if (!this._scalegunToolTransform)  //originally _launcherTransform
            {
                var _foundProbeLauncher = Locator.GetPlayerCamera().GetComponentInChildren<PlayerTool>();  //_foundProbeLauncher can be any tool, but im not gonna change the local var name every goddamn time so
                if (_foundProbeLauncher != null)    //for some reason, when other tools get deployed it does some messy stuff, idfk.

                    this._stowTransform = _foundProbeLauncher._stowTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._stowTransform} from {_foundProbeLauncher}"); //The Transforms don't print into strings like this unfortunately
                this._holdTransform = _foundProbeLauncher._holdTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._holdTransform} from {_foundProbeLauncher}");
                this._moveSpring = _foundProbeLauncher._moveSpring;
            }
            if (!this._alwaysVisible && this._staffProp)
            {
        this._staffProp.SetActive(false);
    } 
    }

    public override bool AllowEquipAnimation()
    {
        base.AllowEquipAnimation();
        return true;
    }
    public override bool HasEquipAnimation()
    {
        return base.HasEquipAnimation();
    }

    // Token: 0x06002349 RID: 9033 RVA: 0x000029DE File Offset: 0x00000BDE


    // Token: 0x0600234A RID: 9034 RVA: 0x0001B442 File Offset: 0x00019642


    public override void EquipTool()
    {
        base.EquipTool();
        base.enabled = true;
       // this._isEquipped = true;
        if (this._staffProp)
        {
            this._staffProp.SetActive(true);
        }





    }

    public void OnEquipGun()
    {
        base.enabled = true;
        //this._equipTime = Time.time;
        this._canvas.enabled = true;
        this._staffObject.SetActive(true);
    }
    public override void UnequipTool()
    {
            Locator.GetToolModeSwapper().UnequipTool();   //This unequips ANYTHING
        base.UnequipTool();
        base.enabled = true;
            //this._isEquipped = false; //shouldn't need doing?
       if (this._staffProp)
        { this._staffProp.SetActive(false); } //NEED TO WAIT FOR PUT-AWAY ANIM TO FINISH
    }

    private void OnscalegunEquipped(ScalegunTool tool)
    {
        if (tool == this)
        {
            Locator.GetPlayerAudioController().PlayEquipTool();
            return;
        }
        //if (this._shareActiveProbes && launcher.SharesActiveProbes() && this. != null)  //References GunInterface
        //{
        //  launcher.SetActiveProbe(this._activeProbe);

    }








        public override bool IsCentered()
        {
            return true;
        }








    }
}


