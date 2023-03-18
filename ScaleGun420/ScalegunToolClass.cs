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
    public class ScalegunToolClass : PlayerTool
    {
        private Transform _sgToolClassTransform; //reference to current attached GO's transform; used by Awake
        public GameObject _sgPropGroupject; //has to be public so ScalegunProp Awake can reference it and assign itself
        public ScalegunPropClass _sgPropClass;


        private void Awake()  
        {
 //GetComponentInChildren doesn't search for inactive objects by default, needs to be set to (true) to find inactive stuff
            _sgPropClass = GetComponentInChildren<ScalegunPropClass>(true);  //Setting it to (true) worked ok fine idk whatever
            if (_sgPropClass == null)
            { ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine("ScalegunToolClass's Awake() method failed to assign _sgPropClass"); }
            if (!this._sgToolClassTransform)
            { this._sgToolClassTransform = base.transform; }  //ProbeLauncher does this
            StealOtherToolTransforms();
        }

        //_sgToolGameobject.SetActive(true); //CURRENT EXPERIMENT: DISABLING 34 AS PART OF GAMEOBJECT PHASEOUT
        //_staffProp.SetActive(false);      //PUT THIS IN ScalegunProp.Awake

        public override void Start() 
        {
            base.Start(); //disables tool by default, even Translator main.  ////031623_2059: error???
        }

        private void StealOtherToolTransforms()
        {
            if (_sgToolClassTransform != null)  //originally launcherTransform.  The fact this is firing successfully suggests _scalegunToolTransform isn't, in fact, null, so presumably neither is the GO
            {
                var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<Signalscope>();  //
                if (_foundToolToStealTransformsFrom != null)    //for some reason, when other tools get deployed it does some messy stuff, idfk.
                    //VIO CONFIRMED THIS IS A BAD IDEA
                    _stowTransform = _foundToolToStealTransformsFrom._stowTransform;  //CONFIRMED THAT STUTTERING OCCURS SWAPPING BETWEEN ScaleGun AND WHATEVER TOOL IT STOLE ITS TRANSFORMS FROM
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._stowTransform} from {_foundToolToStealTransformsFrom}"); //The Transforms don't print into strings like this unfortunately
                _holdTransform = _foundToolToStealTransformsFrom._holdTransform;
                ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._holdTransform} from {_foundToolToStealTransformsFrom}");
                _moveSpring = _foundToolToStealTransformsFrom._moveSpring;
            }
        }

        //THE TWO CONDITIONS NECESSARY FOR THE PlayerTool.Update METHOD TO RUN AT ALL
        public override bool HasEquipAnimation()   //The if() in PlayerTool.Update checks HasEquipAnimation, meaning it calls it.  Meaning it's supposed to be running repeatedly. meaning the spam is intentional.  cry about it.
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
                    _sgPropGroupject.SetActive(true);  //TEST Ln114: disabled because NomaiTranslator doesn't override this at all //update: THIS ISN'T AN OVERRIDE, PlayerTool DOESN'T HAVE AN OnEnable BY DEFAULT
                }
            }
        }
        private void OnDisable()
        {
            _sgPropClass.OnFinishUnequipAnimation();//031623_2054: Error?  nullref with set_enabled?  _sgPropClass undefined?
        }





    }
}


