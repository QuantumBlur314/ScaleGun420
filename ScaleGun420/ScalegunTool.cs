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
    //JUST COPY EVERYTHING OTHER TOOLS DO, EVEN IF THE METHODS ARE EMPTY.  CARGO CULT CODEBASE //Update: my power grows
    public class ScalegunTool : PlayerTool
    {
        private Transform _scalegunToolTransform; //used by Awake
        private GameObject _staffProp;
        private GameObject _sgToolGameobject; //make some debug keybinds to toggle the object active/inactive.  Should be ez


        private void Awake() //Happens naturally at the end of ScaleGun420, when its _sg
        {
            this._scalegunToolTransform = base.transform;  //removed an if //update: check if necessary

            RenderNomaiStaff();
            StealOtherToolTransforms();  //NOT THE CULPRIT

            _sgToolGameobject = ScaleGun420Modbehavior.Instance._sgToolGameobject;
            _sgToolGameobject.SetActive(true); //CURRENT EXPERIMENT //4got 2 assign object //OK so is this already part of the Awake method?  we'll find out if i ever disarble it
            _staffProp.SetActive(false);      //UNNECESSARY????    //Idk brop, NomaiTranslatorProp SetsActive(false) on Awake //Update: moved to AFTER the main object gets activated
        }
 
        public override void Start()
        {
            base.Start();
        }

        private void RenderNomaiStaff()
        {
            LoadStaff();
            _staffProp = Instantiate(GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff"), ScaleGun420Modbehavior.Instance._sgToolGameobject.transform);

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
                var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<PlayerProbeLauncher>();  //
                if (_foundToolToStealTransformsFrom != null)    //for some reason, when other tools get deployed it does some messy stuff, idfk.

                    this._stowTransform = _foundToolToStealTransformsFrom._stowTransform;  //CONFIRMED THAT STUTTERING OCCURS SWAPPING BETWEEN ScaleGun AND WHATEVER TOOL IT STOLE ITS TRANSFORMS FROM
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._stowTransform} from {_foundToolToStealTransformsFrom}"); //The Transforms don't print into strings like this unfortunately
                this._holdTransform = _foundToolToStealTransformsFrom._holdTransform;
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._holdTransform} from {_foundToolToStealTransformsFrom}");
                this._moveSpring = _foundToolToStealTransformsFrom._moveSpring; 
            }
        }

        //THE TWO CONDITIONS NECESSARY FOR THE PlayerTool.Update METHOD TO RUN AT ALL
        public override bool HasEquipAnimation()            //RETURNS TRUE ONLY IF ScalegunTool._stowTransform && ScalegunTool._holdTransform AREN'T NULL; SET THESE!
        {
            bool hasEquipAnimation = (this._stowTransform != null && this._holdTransform != null);
            ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Reminder: something is spam-calling HasEquipAnimation.  returned {hasEquipAnimation} but also wtf");
            return base.HasEquipAnimation();
        }    
        public override bool AllowEquipAnimation()
        {
            return base.AllowEquipAnimation();
        }


        public override void EquipTool()          // IS NEVER BEING CALLED FOR SOME REASON?????
        {
            ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"called ScalegunTool.EquipTool");
            base.EquipTool();
            // this._isEquipped = true;
            //if (this._staffProp)
            {
                //this._staffProp.SetActive(true);  //EXPERIMENT: DISABLING Ln88 AS OnDisable ALREADY RUNS _staffProp.SetActive(true)  //Experiment successful
            }
        }

        public override void UnequipTool()          //CALLED BY ToolModeSwapper.EquipToolMode(ToolMode toolMode), which is itself called by ToolModeSwapper.Update
        {
            ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"called UnequipTool");
            base.UnequipTool(); 
         //base.UnequipTool SETS _isPuttingAway TO TRUE, THEN PlayerTool.Update APPLIES THE STOWTRANSFORMS THEN SETS base.enabled = false ONCE DONE ANIMATING
        }


            //if (this._shareActiveProbes && launcher.SharesActiveProbes() && this. != null)  //References GunInterface
            //{
            //  launcher.SetActiveProbe(this._activeProbe);

        
        public override void Update()
        {
            base.Update();        //PlayerTool's base Update method handles deploy/stow anims; Everything else here is for Scalegun functions
            if (!this._isEquipped || this._isPuttingAway)           //Only does additional stuff if ScalegunTool is equipped.  DISABLED ON A HUNCH  UPDATE HUNCH WAS WRONG, CARRY ON
            {
              return;
            }
        }


        // [[[  P R O P   S T U F F  ]]]

        private void OnEnable()
        {
            {
                if (!PlayerState.AtFlightConsole())        //borrowed from Signalscope.  Idk why different tool props have their OnEnable & OnDisable methods as different access levels
                {   
                    _staffProp.SetActive(true);
                }
            }
        }
        private void OnDisable()      
        {
            _staffProp.SetActive(false);
        }





    }
}


