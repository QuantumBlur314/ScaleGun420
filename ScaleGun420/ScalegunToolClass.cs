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
    //JUST COPY EVERYTHING OTHER TOOLS DO, EVEN IF THE METHODS ARE EMPTY.  CARGO CULT CODEBASE //Update: my power grows  //031923_1845: UPDATE: hubris
    public class ScalegunToolClass : PlayerTool
    {
        private Transform _sgToolClassTransform; //reference to current attached GO's transform; used by Awake
        public ScalegunPropClass _sgPropClass;

        private List<GameObject> _currentLayerSiblings;
        private List<GameObject> _currentObjChildren;
        private bool _atBedrock;
        private bool _atSky;
        private bool _objAlreadySelected;
        private bool _targetHasSiblings;
        private GameObject _theParentOfTarget;
        private GameObject _selectedObject;
        private GameObject _priorSelObject;
        private GameObject _topSibling;
        private GameObject _bottomSibling;


        private void Awake()
        {
            //GetComponentInChildren doesn't search for inactive objects by default, needs to be set to (true) to find inactive stuff
            _sgPropClass = GetComponentInChildren<ScalegunPropClass>(true);  //Setting it to (true) worked ok fine idk whatever

            if (!_sgToolClassTransform)
            { this._sgToolClassTransform = base.transform; }  //ProbeLauncher does this


            StealOtherToolTransforms();
        }


        public override void Start()
        {
            base.Start(); //disables tool by default, even Translator main. 
        }

        private void StealOtherToolTransforms()
        {
            if (_sgToolClassTransform != null)  //originally launcherTransform
            {
                var _foundToolToStealTransformsFrom = Locator.GetPlayerBody().GetComponentInChildren<Signalscope>();  //
                if (_foundToolToStealTransformsFrom != null)
                    //VIO CONFIRMED THIS IS A BAD IDEA
                    _stowTransform = _foundToolToStealTransformsFrom._stowTransform;  //CONFIRMED THAT STUTTERING OCCURS SWAPPING BETWEEN ScaleGun AND WHATEVER TOOL IT STOLE ITS TRANSFORMS FROM
                TheLogGoober.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._stowTransform} from {_foundToolToStealTransformsFrom}"); //The Transforms don't print into strings like this unfortunately
                _holdTransform = _foundToolToStealTransformsFrom._holdTransform;
                TheLogGoober.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._holdTransform} from {_foundToolToStealTransformsFrom}");
                _moveSpring = _foundToolToStealTransformsFrom._moveSpring;  //REMEMBER TO DIG UP WHATEVER FORMAT _moveSpring USES AND MAKE YOUR OWN so it stops fighting the tool it stole from.
            }
        }



        public override void EquipTool()
        {
            TheLogGoober.WriteLine($"called ScalegunTool.EquipTool");
            base.EquipTool();
            this._sgPropClass.OnEquipTool(); //Following in the footsteps of Translator/TranslatorPRop
        }

        public override void UnequipTool()          //CALLED BY ToolModeSwapper.EquipToolMode(ToolMode toolMode), which is itself called by ToolModeSwapper.Update
        {
            base.UnequipTool();
            this._sgPropClass.OnUnequipTool();
            //base.UnequipTool SETS _isPuttingAway TO TRUE, THEN PlayerTool.Update APPLIES THE STOWTRANSFORMS THEN SETS base.enabled = false ONCE DONE ANIMATING
        }

        public override void Update()
        {
            base.Update();        //PlayerTool's base Update method handles deploy/stow anims; Everything else here is for Scalegun functions
            if (!this._isEquipped || this._isPuttingAway)           //Only does additional stuff if ScalegunTool is equipped.  DISABLED ON A HUNCH  UPDATE HUNCH WAS WRONG, CARRY ON
            {
                return;
            }
            EyesDrillHoles();

        }


        public void EyesDrillHoles()
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary) && Locator.GetPlayerCamera() != null && ScaleGun420Modbehavior.Instance._vanillaSwapper.IsInToolMode(ScaleGun420Modbehavior.Instance.SGToolmode))   //031823_1505: Changed a bunch of stuff to __instance for cleanliness; may or may not bork things //031823_1525: Okay so apparently that made it start nullreffing? //REBUILDING IS FAILING, THANKS MICROSOFT.NET FRAMEWORK BUG
            {
                Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

                Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
                var retrievedRootObject = hit.collider.gameObject;
                Intake(retrievedRootObject);
            }

        }

        private void Intake(GameObject seenColliderToCpu)  //Processes initial target found by ScalegunToolClass.EyesDrillHoles
        {
            if (seenColliderToCpu == _selectedObject)
            { return; }
            else if (_priorSelObject == null && _selectedObject != null)
            {
                _priorSelObject = _selectedObject;
                _selectedObject = seenColliderToCpu;
            }
            else if (_priorSelObject == null && _selectedObject == null)
            { _selectedObject = seenColliderToCpu; }
            ProcessScreen();
        }
        private void ProcessScreen()
        { _sgPropClass.SubmitGOs(_selectedObject); }


        public void ClearTerminal()
        {
            StopEditing();
            _selectedObject = null;
            _topSibling = null;
            _bottomSibling = null;
            _theParentOfTarget = null;
            _sgPropClass.SubmitGOs(null);

        }
        public void StopEditing()
        { }



        // [[[  P R O P   S T U F F  ]]]

        private void OnEnable()
        {
            {
                if (!PlayerState.AtFlightConsole())        //borrowed from Signalscope.  Idk why different tool props have their OnEnable & OnDisable methods as different access levels
                {
                    _sgPropClass.enabled = true;  //TEST Ln114: disabled because NomaiTranslator doesn't override this at all //update: THIS ISN'T AN OVERRIDE, PlayerTool DOESN'T HAVE AN OnEnable BY DEFAULT// 031823_0637: disabling since it wasn't in the holy scriptures of NomaiTranslator, idfk anymore// 032123_1917: Disabling because NomaiTranslatorProp doesn't even define a gameobject.
                }
            }
        }
        private void OnDisable() //RUNS _sgPropClass.OnFinishUnequipAnimation, which runs _sgOwnPropGroupject.SetActive(false)
        {
            _sgPropClass.OnFinishUnequipAnimation();//031623_2054: Error?  nullref with set_enabled?  _sgPropClass undefined?
        }





    }
}


