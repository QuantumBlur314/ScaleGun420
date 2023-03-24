using HarmonyLib;
using JetBrains.Annotations;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ScaleGun420.ScaleGun420Modbehavior;  //What the cool kids are doin

namespace ScaleGun420
{
    //JUST COPY EVERYTHING OTHER TOOLS DO, EVEN IF THE METHODS ARE EMPTY.  CARGO CULT CODEBASE //Update: my power grows  //031923_1845: UPDATE: hubris
    public class ScalegunToolClass : PlayerTool
    {
        private Transform _sgToolClassTransform; //reference to current attached GO's transform; used by Awake
        public ScalegunPropClass _sgPropClass;

        public static List<GameObject> _selObjSiblings;
        private List<GameObject> _currentObjChildren;
        private bool _atBedrock;
        private bool _atSky;
        private bool _isInEditMode = false;
        private bool _targetHasSiblings;
        private GameObject _parentOfSelection;
        public static GameObject _selectedObject; 
        public static GameObject _previousSelection;  //not used here, but ScalegunPropClass will use it to fill in adjacent UI fields without having to recalculate, //032323_1938: Actually this should probably be defined by the PropClass
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
                LogGoob.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._stowTransform} from {_foundToolToStealTransformsFrom}"); //The Transforms don't print into strings like this unfortunately
                _holdTransform = _foundToolToStealTransformsFrom._holdTransform;
                LogGoob.WriteLine($"Successfully stole {_foundToolToStealTransformsFrom._holdTransform} from {_foundToolToStealTransformsFrom}");
                _moveSpring = _foundToolToStealTransformsFrom._moveSpring;  //REMEMBER TO DIG UP WHATEVER FORMAT _moveSpring USES AND MAKE YOUR OWN so it stops fighting the tool it stole from.
            }
        }



        public override void EquipTool()
        {
            LogGoob.WriteLine($"called ScalegunTool.EquipTool");
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
            if (ScaleGun420Modbehavior.Instance._vanillaSwapper.IsInToolMode(ScaleGun420Modbehavior.Instance.SGToolmode) && OWInput.IsInputMode(InputMode.Character))
            {
                if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary) && !_isInEditMode)   //031823_1505: Changed a bunch of stuff to __instance for cleanliness; may or may not bork things //031823_1525: Okay so apparently that made it start nullreffing? //REBUILDING IS FAILING, THANKS MICROSOFT.NET FRAMEWORK BUG
                {
                    EyesDrillHoles();
                }

                if (_selObjSiblings != null && _selObjSiblings.Count > 1)
                {
                    if (UpBubbon)            //032223_1747: nullref???  //032323_1753: Setting the Bubbon bools static in Modbehavior lets me not need to do .Instance (with help from the Using: above)
                    {
                        ScrollSiblingList(1);
                        _sgPropClass.UpdateScreenText();
                        //_sgPropClass.OnScrollUpSiblings();

                        
                    }
                    else if (DownBubbon)
                    {
                        ScrollSiblingList(-1);
                        _sgPropClass.UpdateScreenText();
                    
                    }
                }

                //var siblingIndex = _selectedObject.transform.GetSiblingIndex();
               // var list = _selectedObject.transform.parent.GetChild(siblingIndex);
               // _sgPropClass.UpdateScreenText(_selectedObject, siblingIndex);
            }
        }





        public void ScrollSiblingList(int increment = 1)
        {
            if (_selObjSiblings.Count <= 1) //Update already checks this but idk
            { return; }

            _previousSelection = _selectedObject;   //how do i account for the list changing without having to rerun GetSiblings?  idk

            var myItem = _selObjSiblings[(increment + 1) % _selObjSiblings.Count];  //0323_1519: Idiot says this will always wrap around the list using "modulo" and Corby says to use .Count since .Count() will return Linq which is "stinky"
            _selectedObject = myItem;
        }



        private void EyesDrillHoles()
        {
            Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

            Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
            var retrievedRootObject = hit.collider.gameObject.transform.parent.gameObject;

            if (_selectedObject != null && retrievedRootObject == _selectedObject)
            { return; }

            _previousSelection = _selectedObject;
            _selectedObject = retrievedRootObject;

            _selObjSiblings = _selectedObject.GetSiblings();
            _sgPropClass.UpdateScreenText();
        }


        public void CycleIntoSelectQueue(GameObject newSelection)
        {
            if (newSelection == _previousSelection)
            {
                Instance.ModHelper.Console.WriteLine($"{newSelection} is already _previousSelection, this should never happen");
                return;
            }
        }


        public void ClearTerminal()
        {
            StopEditing();
            _selectedObject = null;
            _topSibling = null;
            _bottomSibling = null;
            _parentOfSelection = null;
            _sgPropClass.UpdateScreenText();

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


