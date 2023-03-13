using HarmonyLib;
using JetBrains.Annotations;
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
    //JUST COPY EVERYTHING OTHER TOOLS DO, EVEN IF THE METHODS ARE EMPTY.  CARGO CULT CODEBASE
    public class ScalegunTool : PlayerTool
    {
        private Transform _scalegunToolTransform; //used by Awake
        public GameObject _staffProp;
        public GameObject _staffGameobject; //make some debug keybinds to toggle the object active/inactive.  Should be ez


        private void Awake()         //IS AWAKE EVEN BEING CALLED?
        {
            this._scalegunToolTransform = base.transform;  //removed an if

            RenderNomaiStaff();
            StealOtherToolTransforms();  //NOT THE CULPRIT
            _staffProp.SetActive(false);      //UNNECESSARY????    //Idk brop, NomaiTranslatorProp SetsActive(false) on Awake
        }
 
        public override void Start()
        {
            base.Start();
            base.enabled = true;  //UNNECESSARY?
        }


        public void RenderNomaiStaff()
        {
            LoadStaff();
            _staffProp = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"), ScaleGun420.Instance._sgToolGameobject.transform);
            _staffProp.transform.localPosition = new Vector3(0.5496f, -1.11f, -0.119f);
            _staffProp.transform.localEulerAngles = new Vector3(343.8753f, 200.2473f, 345.2718f);
            var streamingRenderMeshHandle = _staffProp.GetComponentInChildren<StreamingRenderMeshHandle>();
            streamingRenderMeshHandle.OnMeshUnloaded += LoadStaff;
            void LoadStaff() { StreamingManager.LoadStreamingAssets("brittlehollow/meshes/props"); }
        }
        private void StealOtherToolTransforms()
        {
            if (this._scalegunToolTransform != null)  //originally _launcherTransform
            {
                var _foundProbeLauncher = Locator.GetPlayerBody().GetComponentInChildren<Signalscope>();  //_foundProbeLauncher can be any tool, but im not gonna change the local var name every goddamn time so
                if (_foundProbeLauncher != null)    //for some reason, when other tools get deployed it does some messy stuff, idfk.

                    this._stowTransform = _foundProbeLauncher._stowTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._stowTransform.ToString()} from {_foundProbeLauncher}"); //The Transforms don't print into strings like this unfortunately
                this._holdTransform = _foundProbeLauncher._holdTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._holdTransform} from {_foundProbeLauncher}");
                this._moveSpring = _foundProbeLauncher._moveSpring;  //NONE OF THIS IS RESPONSIBLE FOR THE LOOP
            }
        }


        //THE TWO CONDITIONS NECESSARY FOR THE PlayerTool.Update METHOD TO RUN AT ALL
        public override bool HasEquipAnimation()            //RETURNS TRUE ONLY IF ScalegunTool._stowTransform && ScalegunTool._holdTransform AREN'T NULL; SET THESE!
        {
            bool hasEquipAnimation = (this._stowTransform != null && this._holdTransform != null);
            ScaleGun420.Instance.ModHelper.Console.WriteLine($"Reminder: HasEquipAnimation returned {hasEquipAnimation} and will continue to do so until you sort this out");
            return base.HasEquipAnimation();
        }
        public override bool AllowEquipAnimation()
        {
            return base.AllowEquipAnimation();
        }


        public override void EquipTool()          // IS NEVER BEING CALLED FOR SOME REASON?????
        {
            ScaleGun420.Instance.ModHelper.Console.WriteLine($"called ScalegunTool.EquipTool");
            base.EquipTool();

            //base.enabled = true;  ALREADY PART OF base.EquipTool
            // this._isEquipped = true;
            if (this._staffProp)
            {
                this._staffProp.SetActive(true);
            }
        }

        public override void UnequipTool()          //CALLED BY ToolModeSwapper.EquipToolMode(ToolMode toolMode), which is itself called by ToolModeSwapper.Update
        {
            ScaleGun420.Instance.ModHelper.Console.WriteLine($"called UnequipTool");
            base.UnequipTool();         //nothing's being called   

            base.enabled = false;         //changed from "true" to "false" idk why tho //base.UnequipTool SETS _isPuttingAway TO TRUE, THEN PlayerTool.Update APPLIES THE STOWTRANSFORMS THEN SETS base.enabled = false ONCE DONE ANIMATING
            //if (this._staffProp)
           // { this._staffProp.SetActive(false); } //NEED TO WAIT FOR PUT-AWAY ANIM TO FINISH
        }


            //if (this._shareActiveProbes && launcher.SharesActiveProbes() && this. != null)  //References GunInterface
            //{
            //  launcher.SetActiveProbe(this._activeProbe);

        
        public override void Update()           //PlayerTool's default update handles deploy animations and nothing else.  I've confirmed it's running.
        {
            base.Update();        //DEPLOY ANIMS HANDLED BY PlayerTool.Update; Everything else here is for Scalegun functions
            if (!this._isEquipped || this._isPuttingAway)           //Only does additional stuff if ScalegunTool is equipped.  DISABLED ON A HUNCH  UPDATE HUNCH WAS WRONG, CARRY ON
            {
              return;
            }
        }


        //REDUNDANT PlayerTool base. CLASS STUFF TO DEBUG WHATEVER'S BREAKING.  ONCE EVERYTHING WORKS, ALL THIS IS SAFE TO DELETE //UPDATE: ALL IRRELEVANT GARBAGE HAS BEEN CYCLED OUT

        // [[[  P R O P   S T U F F  ]]]



        private void OnEnable()
        {
            {
                if (!PlayerState.AtFlightConsole())        //borrowed from Signalscope.  Idk why different tool props have their OnEnable & OnDisable methods as different access levels
                {   
                    ScaleGun420.Instance._sgToolGameobject.SetActive(true);  //NEITHER NECESSARY NOR KOSHER, SIGNALSCOPE DOESN'T DO THIS AND NEITHER SHOULD I //FUCK YOU
                    _staffProp.SetActive(true);    //idk idk idk
                    ScaleGun420.Instance.ModHelper.Console.WriteLine("Called ScalegunTool.OnEnable.  Set _staffProp to 'active' (ALLEGEDLY)"); //MESSAGE RECEIVED ON WORLD LOADED, HMM
                }
            }
        }
        private void OnDisable()      //private void per 
        {
            _staffProp.SetActive(false); //NEW, DISABLED BELOW BECAUSE SUCKS AND BAD I THINK
            //{ ScaleGun420.Instance._sgToolGameobject.SetActive(false); }
        }





    }
}


