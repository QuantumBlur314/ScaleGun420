using HarmonyLib;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{

    public class ScalegunTool : PlayerTool     //Remember to account for times when the game auto-deploys the Scout Launcher //Edit2: NHExamples makes it clear that it's probably easier/default
    {
        protected Transform _scalegunToolTransform; //used by Awake
        private GameObject _staffObject;
        private bool _justEquipped;
        private bool _alwaysVisible;
        private Canvas _canvas;
        private float _equipTime;

        //help


        //Do I have to define PlayerTool._stowTransform and _holdTransform?  
        private void Awake()
        {
            if (!this._scalegunToolTransform)  //originally _launcherTransform
            {
                var _foundProbeLauncher = Locator.GetPlayerCamera().GetComponentInChildren<PlayerProbeLauncher>();
                if (_foundProbeLauncher != null)

                    this._stowTransform = _foundProbeLauncher._stowTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._stowTransform} from {_foundProbeLauncher}");
                this._holdTransform = _foundProbeLauncher._holdTransform;
                ScaleGun420.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundProbeLauncher._holdTransform} from {_foundProbeLauncher}");

                this._scalegunToolTransform = _foundProbeLauncher._launcherTransform;
            }
            if (!this._alwaysVisible && this._staffObject)
            {
                this._staffObject.SetActive(false);
            }
        }
        public override void Start()
        {
            base.Start();
            base.enabled = true;

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
        protected virtual void OnDisable()
        {
            if (!this._alwaysVisible && this._staffObject)
            {
                this._staffObject.SetActive(false);
            }
        }

        public override void EquipTool()
        {
            base.EquipTool();
            base.enabled = true;
            this._justEquipped = true;
            if (this._staffObject)
            {
                this._staffObject.SetActive(true);
            }





        }

        public void OnEquipGun()
        {
            base.enabled = true;
            this._equipTime = Time.time;
            this._canvas.enabled = true;
            this._staffObject.SetActive(true);
        }
        public override void UnequipTool()
        {
            base.UnequipTool();
            base.enabled = true;
            if (this._staffObject)
            { this._staffObject.SetActive(false); }
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



    }
}


