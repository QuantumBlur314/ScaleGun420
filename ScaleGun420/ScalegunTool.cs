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
        private Transform _sgToolClassTransform; //reference to current attached GO's transform; used by Awake
        private GameObject _sgToolGameobject; //to be set as the Gameobject this class is attached to.
        public GameObject _sgPropGO; //has to be public so ScalegunProp Awake can reference it and assign itself
        //private GameObject _sgToolGameobject; //make some debug keybinds to toggle the object active/inactive.  Should be ez// IT MAKES NO MEANINGFUL EDITS TO ITS OWN 
        public ScalegunProp _sgPropClass;


        private void Awake()  //Happens at the end of ScaleGun420Modbehavior.
        {
            _sgToolGameobject = ScaleGun420Modbehavior.Instance._sgToolGameobject;  //sets self to the same ScalegunTool established by the main modbehavior class
            //this._sgToolClassTransform = base.transform;  //removed an if //update: check if necessary. 
            //this._sgPropClass = ScaleGun420Modbehavior.Instance._sgToolGameobject.AddComponent<ScalegunProp>();  // addcomponent done like I did with the main _sgToolGameobject in ScalegunInit, it worked and cleared a nullref
            //if (_sgPropClass == null) { ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"_sgProp wasn't set to ScalegunProp, it is {_sgPropClass}"); }  //RETURNED EMPTY ALREADY

            StealOtherToolTransforms();
            _sgPropGO = _sgToolGameobject.SpawnChildGOAtParent("ScaleGunPropGroup"); //scalegunPropGroup exists only when the ScalegunTool class is active and enabled, which it doesn't become by default?
            //FIGURE OUT WHAT ACTIVATES/FAILS TO ACTIVATE ScalegunTool (THE CS YOU'RE CURRENTLY LOOKING AT)

            if (_sgPropGO != null)
            { ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine("ScalegunTool._sgToolGameobject birthesd _sgPropGO"); }

            _sgPropClass = _sgPropGO.AddComponent<ScalegunProp>();  //error

            if (_sgPropClass != null)
            { ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine("ScalegunTool._sgPropClass assigned to _sgPropGO"); }
        }

        //_sgToolGameobject.SetActive(true); //CURRENT EXPERIMENT: DISABLING 34 AS PART OF GAMEOBJECT PHASEOUT
        //_staffProp.SetActive(false);      //PUT THIS IN ScalegunProp.Awake

        public override void Start()
        {
            base.Start();
        }

        private void StealOtherToolTransforms()
        {
            if (this._sgToolClassTransform != null)  //originally launcherTransform.  The fact this is firing successfully suggests _scalegunToolTransform isn't, in fact, null, so presumably neither is the GO
            {
                var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<PlayerProbeLauncher>();  //
                if (_foundToolToStealTransformsFrom != null)    //for some reason, when other tools get deployed it does some messy stuff, idfk.
                    //VIO CONFIRMED THIS IS A BAD IDEA
                    this._stowTransform = _foundToolToStealTransformsFrom._stowTransform;  //CONFIRMED THAT STUTTERING OCCURS SWAPPING BETWEEN ScaleGun AND WHATEVER TOOL IT STOLE ITS TRANSFORMS FROM
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._stowTransform} from {_foundToolToStealTransformsFrom}"); //The Transforms don't print into strings like this unfortunately
                this._holdTransform = _foundToolToStealTransformsFrom._holdTransform;
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._holdTransform} from {_foundToolToStealTransformsFrom}");
                this._moveSpring = _foundToolToStealTransformsFrom._moveSpring;
            }
        }

        //THE TWO CONDITIONS NECESSARY FOR THE PlayerTool.Update METHOD TO RUN AT ALL
        public override bool HasEquipAnimation()            //The if() in PlayerTool.Update checks HasEquipAnimation, meaning it calls it.  Meaning it's supposed to be running repeatedly. meaning the spam is intentional.  cry about it.
        {
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
            this._sgPropClass.OnEquipTool(); //Following in the footsteps of Translator/TranslatorPRop
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
            this._sgPropClass.OnUnequipTool();
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
                    _sgPropGO.SetActive(true);  //TEST Ln114: disabled because NomaiTranslator doesn't override this at all //update: THIS ISN'T AN OVERRIDE, PlayerTool DOESN'T HAVE AN OnEnable BY DEFAULT
                }
            }
        }
        private void OnDisable()
        {
            this._sgPropClass.OnFinishUnequipAnimation();
        }





    }
}


