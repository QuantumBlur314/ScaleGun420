﻿using Newtonsoft.Json.Serialization;
using OWML.Common;
using System;
using System.CodeDom;
using System.Threading.Tasks;
using System.Text;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ScaleGun420.Extensions;  //had to do this for some bs reason idfk


namespace ScaleGun420
{

    //041023_1557: SUCCESSFULlY made _selectedGOPublic only update upon completion of processes, using internal fields where necessary instead
    /// <summary>
    /// MISSION UPDATE: Needlessly complex, but perhaps prevent going down paths that exclusively contain colliders?
    /// </summary>
    public class SgComputer : MonoBehaviour
    {
        private ScalegunPropClass _sgPropClass;
        //private ScalegunToolClass _sgToolClass;

        private List<GameObject> _siblingsOfSelGO;
        private List<GameObject> _childGOList;
        ///public static ObservableCollection<GameObject> _observableCollectionTest;  //subscribe to the CollectionChanged event.  Event's arguments are NotifyCollectionChangedArgs.  Might be helpful idk

        private bool _babenCycleShouldRun = false;
        private Coroutine timerBabyCycle = null;

        private bool _onToChildsBeganThisCoroutine = false;
        private bool _loadingKids_OnToAdjSibsBehalf = false;

        public Coroutine timerChildrenPending = null;
        public Coroutine timerLoadingSiblings = null;

        public bool _cancelLoadChildren = false;
        public bool _cancelLoadSiblings = false;

        private bool _pauseBabenCycleUntilLoaded = false;

        private int _hasScrolledToParentXTimes = 0;

        private float _timeBeforeChildrenLoad;
        private float _timeBeforeSiblingsLoad;

        //public static GameObject _previousSelection;  //not used here, but ScalegunPropClass will use it to fill in adjacent UI fields without having to recalculate, //032323_1938: Actually this should probably be defined by the PropClass
        //private GameObject _parentOfSelection;

        public GameObject _selectedGOPublic;

        private GameObject _currentSelInternal_onToChild;
        private GameObject _finalSelection_OnToChild;
        private GameObject _freshSelInternal_OnToParent;

        private GameObject _candidateInternal_onToChilds;   //THIS ISN'T GETTING UPDATED

        public int _selecIndex = 0;
        private int _indexDisplayedChild = 0;

        private float _coroutineTimerStartValueUniv = 1f;
        private float _subsequentPressIncrementUniv = 0.5f;
        public ProbeLauncherEffects _probeLauncherEffects;

        public static string _colliderFilter = "Collider";
        public List<string> _forbiddenObjects = new List<string>() { "Collider" };



        //CLASS IS ENABLING LATE, ONLY ON EQUIP; ALSO, CONSIDER DEACTIVATING COMPUTER WHILE TOOL IS UNEQUIPPED
        private void Awake()
        {
            LogGoob.WriteLine("SgComputer is woke, grabbing ScalegunPropClass...", MessageType.Success);
            //_sgToolClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>();
            _sgPropClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunPropClass>();
            _probeLauncherEffects = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncherEffects>();
        }
        private void Start()
        { base.enabled = false; }

        private void AAADebug_SetNewTimerValues(float timerStartValues = 1f, float subsequentPressValues = 0.5f)
        {
            _coroutineTimerStartValueUniv = timerStartValues;
            _subsequentPressIncrementUniv = subsequentPressValues;
        }

        public enum IndexMarkerState
        {
            ToParentSecondScroll = -69,
        }
        private enum CoroutineStartedBy
        {

        }
        private enum DelayLoadingOf
        {
            Siblings = 0,
            Children = 1,
        }

        public bool CanEnterEditMode()
        {
            if (_selectedGOPublic == null)
            {
                LogGoob.WriteLine("SgComputer.CanEnterEditMode ~100: Was called when _selectedObjectPublic was null; you shouldn't be doing that anywhere");
                return false;
            }
            if (_selectedGOPublic.ToString().Contains(_colliderFilter))
            {
                LogGoob.Scream("Inadvisable to edit colliders");
                return false;
            }
            return true;
        }

        private void SetPubSelGOFieldPlusIndexOption(GameObject setToThis, bool updateSelGOIndexToo = true)
        {
            _selectedGOPublic = setToThis;
            if (updateSelGOIndexToo)
                SetSelGOIndexVia(_selectedGOPublic);
        }
        private void SetSelGOIndexVia(GameObject thisGO)
        { _selecIndex = thisGO.transform.GetSiblingIndex(); }
        private void FlushChildListPlusIndex(bool resetChildIndex = false)
        {
            _childGOList = null;
            if (resetChildIndex)
                _indexDisplayedChild = 0;
        }

        private bool AreSiblingsLoaded()
        { return _siblingsOfSelGO != null; }
        private GameObject GetPubSelection()
        { return _selectedGOPublic; }


        /// <summary>
        /// The override here sucks tbh, not intuitive enough
        /// </summary>
        /// <param name="optionalObjectToCompare"></param>
        /// <param name="setSelGOPub"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GameObject GetGOAtSelecIndexCheck(GameObject optionalObjectToCompare = null, bool setSelGOPub = false)  //_siblingsOfSelGO starts null  
        {
            GameObject foundObject = _selecIndex.FindIndexedGOIn(_siblingsOfSelGO);
            if (optionalObjectToCompare == null || foundObject == optionalObjectToCompare)
            {
                if (setSelGOPub == true)
                    SetSelGOIndexVia(foundObject);
                return foundObject;
            }
            throw new Exception($"SelectionAtSiblingIndex Comparror: foundObject {foundObject} and optionalObjectToCompare {optionalObjectToCompare} weren't the same");
        }

        //Currently starts running on Up or DownSiblings if _timeLeftChildren isn't already running

        public void NavToParent()
        {
            if (timerChildrenPending != null || timerLoadingSiblings != null)
            {
                LogGoob.Scream("Scrolling up hierarchy of unloaded family inadvisable");
                return;
            }
            GameObject existingInternalSelection = _freshSelInternal_OnToParent;
            GameObject originalGOThisPress;

            if (_hasScrolledToParentXTimes <= 0 && existingInternalSelection == null)
                originalGOThisPress = GetPubSelection();
            else if (_hasScrolledToParentXTimes >= 1 && existingInternalSelection != null)
                originalGOThisPress = existingInternalSelection;
            else
                throw new Exception($"OnToParent 145ish: Unaccounted-for scrollstate, setting originalGOThisPress from GetPubSelection().  {_hasScrolledToParentXTimes}, {existingInternalSelection}");
            //now no longer dumb, but very messy  //now no longer messy

            GameObject originalParentThisPress = originalGOThisPress.transform.parent.gameObject;  //maybe just an extension that slaps another .transform.parent.gameObject to the end of the previous, for however many times you press Up?  sounds easier
            if (originalParentThisPress != null)
            {
                GameObject firstGrandparent = originalParentThisPress.transform.parent.gameObject;
                if (firstGrandparent != null) //FOR REFERENCE, _siblingsOfSelGO DOESN'T UPDATE UNTIL THE COROUTINE; _siblingsOfSelGO IS THE LIFELINE THAT _selecIndex CLINGS TO HERE
                {
                    _babenCycleShouldRun = false;

                    string upperSibling = "";
                    string lowerSibling = "";
                    _indexDisplayedChild = 0;

                    //______The First Press_________
                    if (_hasScrolledToParentXTimes < 1 && timerLoadingSiblings == null && AreSiblingsLoaded() == true) /// && originalGOThisPress != null)  //Doesn't require _childGOList to be defined actually, surprising
                    {
                        CopySiblingsToChildList();   //The aforementioned flush lets us push the current siblings into the child field without issue,
                        int unchangedSelIndexForChildField = _selecIndex;
                        _indexDisplayedChild = unchangedSelIndexForChildField; //And now we can tell it to select the one we were on, possibly needless but if Babens start Cycling somehow, it'll be happy

                        //gObjAtSelObjIndex = SelectedGOAtIndex(); //Updates _selectedObject for future use by the else{} statement below this "if"
                        upperSibling = $"Waiting to Load Siblings...";
                        lowerSibling = "Waiting to Load Siblings...";
                        timerLoadingSiblings = StartCoroutine(LoadSiblingsAfter(_coroutineTimerStartValueUniv));  //handles the rest, retrieves fresh _selectedObject index if unavailable; also can probably use _selectObject unless u wanna fuckin... inject it as a parameter

                        if (_hasScrolledToParentXTimes != 0)
                            LogGoob.Scream($"OnToParent Ln175: timerLoadingSiblings was null, but _hasScrolledToParentXTimes was already at {_hasScrolledToParentXTimes}??", MessageType.Error);
                        _hasScrolledToParentXTimes = 1;
                    }
                    else if (_hasScrolledToParentXTimes >= 1 && timerLoadingSiblings != null && AreSiblingsLoaded() == false) /// && originalGOThisPress == null) //whar?
                    {
                        _timeBeforeSiblingsLoad += _subsequentPressIncrementUniv;
                        _hasScrolledToParentXTimes += 1;

                        if (_hasScrolledToParentXTimes >= 2)
                        {
                            FlushChildListPlusIndex();
                            _selecIndex = 0; //(int)IndexMarkerState.ToParentSecondScroll;  //SET THIS NULL WHENEVER YOU NEED TO START FROM 0
                        }///  _arbitraryChildIndex = 0; //already handled by first press actually nvm
                    }
                    else
                    {
                        LogGoob.WriteLine($"OnToParent ~215: OnToParent ERROR, {_hasScrolledToParentXTimes}, {timerLoadingSiblings}, {AreSiblingsLoaded()}", MessageType.Warning);
                        _sgPropClass.RefreshScreen("SKIP", "OnToParent ERROR", "Milk || Cigarettes", "SKIP");
                        return;
                    }

                    GameObject newSelectionAfterPress = originalParentThisPress;
                    _freshSelInternal_OnToParent = newSelectionAfterPress;

                    string newParent = GOToStringOrElse(firstGrandparent);
                    string newChild = GOToStringOrElse(originalGOThisPress, "ERROR");
                    string newSelection = $"{newSelectionAfterPress}";  //nullcheck already exists 

                    _sgPropClass.RefreshScreen(newParent, upperSibling, lowerSibling, newChild, newSelection);
                }
                else
                {
                    LogGoob.Scream("CANNOT BREACH FIRMAMENT", MessageType.Info);
                    return;
                }
            }
            else
            {
                LogGoob.Scream("HOW DID YOU GET HERE", MessageType.Warning);
                return;
            }
        }

        //Does injecting a field into a parameter only set the parameter's initial value, or does it check the field every time the parameter's used in the method?  If _selectedObject changes between when this coroutine starts and when the timer runs out, will it use the CURRENT _selectedObject, or will it have the value _selectedObject had when the coroutine started?
        //040523_1749: Corby confirms it's just like setting a var - it's a one-time copying of the field's value at that moment, and doesn't update.
        /// <summary>
        /// EXCLUSIVELY used by NavToParent
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private IEnumerator LoadSiblingsAfter(float time)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeBeforeSiblingsLoad = time;  //the start of a coroutine only happens once, confirmed
            while (_timeBeforeSiblingsLoad >= 0)
            {
                if (_cancelLoadSiblings)
                {
                    _cancelLoadSiblings = false;
                    yield break;
                }
                _timeBeforeSiblingsLoad -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            //bool onlyScrolledUpOneLayer = (_childGOList == _siblingsOfSelGO);
            //bool scrolledUpMultipleTimes = (_selecIndex == (int)IndexMarkerState.ToParentSecondScroll);  //this is 

            string selectionField = $"ax15_computer._selectedObjectPublic_ax15";
            string upperSibling = "";
            string lowerSibling = "";

            GameObject internalStoredSelection = _freshSelInternal_OnToParent;


            if (_hasScrolledToParentXTimes >= 2)
            {
                selectionField = $"{internalStoredSelection}"; //probably maybe make this whole thingus its own method?  it'd have to pass out lots of fellas tho, it'd have to push lots of dudes through its 
                _childGOList = internalStoredSelection.ListChildrenOrNull();  //for the record, ToParent should ALWAYS generate a child list; if it goes blank exclusively from scrolling ToPArents, then something broke
            }

            _freshSelInternal_OnToParent = null;
            _hasScrolledToParentXTimes = 0;

            int newSelIndex = internalStoredSelection.transform.GetSiblingIndex();  //Now handled by SetPubSelGOFieldPlusIndexOption()'s default parameters //nvm 
            _selecIndex = newSelIndex;

            List<GameObject> newSiblingsList = internalStoredSelection.GetAllSiblings();
            _siblingsOfSelGO = newSiblingsList;

            if (newSiblingsList.Count > 1)
            {
                newSelIndex.TextFromAdjacentSiblingsIn(newSiblingsList, out upperSibling, out lowerSibling);
                //upperSibling = $"{newSelIndex.AdjacentSiblingIn(newSiblingsList, 1)}";
                // lowerSibling = $"{newSelIndex.AdjacentSiblingIn(newSiblingsList, -1)}";
            }

            SetPubSelGOFieldPlusIndexOption(internalStoredSelection, false);
            _sgPropClass.RefreshScreen("SKIP", lowerSibling, upperSibling, "SKIP", selectionField);
            StopCoroutineStartBabies(ref timerLoadingSiblings);
        }


        /// <summary>
        /// EVERY PRESS SETS _selObjPublic?  YEP
        /// 
        /// OnToChilds CAN ONLY RUN ONCE PER LoadChildsAfter() ACTIVATION
        /// 
        /// Either OnToChilds OR NavToSibling can start the LoadChildsAfter() timerChildrenPending coroutine,
        /// but ONLY NavToSibling can happen again while that's running.
        /// 
        /// Pressing OnToChilds, then quickly using NavToSibling, can extend the time before a new child list is loaded,
        /// saving the creation of the new list of children, until the user's settled on the sibling to generate the childlist FROM,
        /// A WHOPPING ONE(1) List<> less than a normal, sane system would've had to generate.
        /// This is a negligible microoptimization.
        /// The whole idea behind this class is negligible microoptimizations.
        /// If you're not a fan of very small red-and-blue Peterbilts then get out of my goddamn zone
        /// 
        /// NavToSibling NEVER CLEARS _childGOList, SINCE _timerLoadingChildren SAVES THE LIST IN CASE USER SCROLLS TO SAME SPOT
        /// but if OnToChilds has been run at all, it probably shouldn't do any of that since guh

        /// 
        /// NavToParent probably shouldn't run then either, ohhhh
        /// </summary>
        // When NavToChild has been run, what conditions does it change?
        // it can nullify the childlist, that's a good signal.  not great for code readability tho
        //maybe a flag or a flog
        //or perchance a dog

        //UNDER CONSTRUCTION: true
        public void NavToChild() //Add condition for scrolling to very bottom of the well
        {
            if (_childGOList == null || timerChildrenPending != null || _onToChildsBeganThisCoroutine)  //Can't gatekeep whether timerChildrenPending's running here, or else it will just skip the whole function
                return;
            _babenCycleShouldRun = false;

            //wait, we WANT it to gatekeep the whole function.  If the timerChildrenPending is running, it means the Children are Loading (i.e. not ready to be scrolled to)
            List<GameObject> childListAtPress = _childGOList;
            int kidIndexAtPress = _indexDisplayedChild;

            GameObject newCandidateFromChild = kidIndexAtPress.FindIndexedGOIn(childListAtPress) ?? throw new System.Exception("OnToChilds: couldn't find GO @ _childIndexCurrent for any number of reasons.  check logs i guess.");
            GameObject priorSelectionGO = _selectedGOPublic;
            //_vvv  an update  vvv_ You don't update selecIndex or sibling list until a few lines down, why are you even using GetGOAtSelecIndexCheck here????
            //upon pressing NavToChild after selecting slate(parentless), apparently newCandidateFromChild was correct, but the index wasn't                    //foundObject was the same as before the press, didn't yield the child somehow
            timerChildrenPending = StartCoroutine(LoadChildrenAfter(_coroutineTimerStartValueUniv, priorSelectionGO));
            _onToChildsBeganThisCoroutine = true;

            _candidateInternal_onToChilds = newCandidateFromChild; //wtf is this  //
            _siblingsOfSelGO = childListAtPress;
            _selecIndex = kidIndexAtPress;

            FlushChildListPlusIndex(true);
            SetPubSelGOFieldPlusIndexOption(newCandidateFromChild); //PENDING

            string siblingAbove = "";
            string siblingBelow = "";
            if (newCandidateFromChild.transform.parent.childCount > 1)
                newCandidateFromChild.TextFromAdjacentSiblingsIn(childListAtPress,
                    out siblingAbove,
                    out siblingBelow, "SKIP");


            ///why must this bastard so insistently nullref
            _sgPropClass.RefreshScreen(
                $"{priorSelectionGO}, prevsel",
                siblingAbove,
                siblingBelow,
                "pending update...",  // ____ .   .   .   Y   O   U   .   .   .____  ...are probably being overwritten by the stuff in LoadChildrenAfter
                $"{newCandidateFromChild}");
        }

        //_hasScrolledToParentXTimes = 1;
        //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first




        //SetPubSelGOFieldPlusIndexOption(_currentSelInternal_onToChild);  //OnToChild SHOULDN'T EVEN START IF NO CHILD EXISTs;DO I NEED THIS INTERNAL VALUE FOR NavToSibling?!

        //WHAT DO YOU MEAN GetChild() BY INDEX ALREADY EXISTS
        //Really wanna make _selectedObject local but The Brain Wall, The Bricks



        //_indexDisplayedChild = 0;  //this shafts an insignificant cosmetic feature in LoadChildrenAfter; is it worth it? //YES, BURN
        ///_selGO_Children = _selectedGOPublic.GetChildListElseNull();  //this ruins the whole "delay loading of new child list until after coroutine" plan, but whatever
        // var newChildList = _childGOList;  ///children are gone wgat are you doing
        //string nextChildText = " d"; //this will not be updated here if coroutine works as planned
        // if (newChildList != null)
        //    nextChildText = $"{newChildList[0]}";  //FindIndexedGOIn isn't necessary



        //if scrolled multiple times, then shouldn't've scrolled multiple times, wtf is wrong with you.

        //CAN I GO TO A CHILD, THEN DELAY THE LOADING OF ITS CHILDREN UNTIL I SCROLL TO THE DESIRED SPOT?!  CAN I SUSPEND THE TIMER IN LIMBO LIKE THAT?!  //040223_2006: More trouble than it's worth for now, maybe later.  Just deal with that list being generated and pray it doesn't lag.
        // _childGOList = null;  //DON'T DO THIS YET, WaitBeforeLoading CAN COMPARE _childGOList with _siblingsOfSelGO (a comparison probably more computationally intensive than it's worth tbh)
        //if (timer == null)                      
        //{ timer = StartCoroutine(WaitBeforeLoading(DelayLoadingOf.Children, 0.5f)); }
        // else { LogGoob.WriteLine("ToolClass Ln236: ToChilds: Timer wasn't null; didn't start WaitBeforeLoading of Children, or do anything else"); }




        //for some reason, this never terminates because its time somehow stops above 0 and it stays in limbo
        //THROWN EXCEPTIONS CAUSE ANY METHOD, POSSIBLY INCLUDING COROUTINES, TO STOP IN THEIR TRACKS.  THAT MIGHT BE WHY THE COUNTER'S GETTING STUCK ABOVE ZERO
        /// <summary>
        /// Not directly responsible for actually handling Current Selected objects, LoadChildrenAfter is only concerned with the delayed
        /// loading of the children of whatever object the user settles upon in their browsing.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="originalSelGOWhenCoroutineStarted"></param>
        /// <returns></returns>
        private IEnumerator LoadChildrenAfter(float time, GameObject originalSelGOWhenCoroutineStarted = null)  //if multiple things call this, ensure each variant waits for other variants to finish to avoid chaos
        {
            _timeBeforeChildrenLoad = time;   //WHY IN GOD'S NAME IS _timeLeftChildren STUCK ABOVE 0, THIS ISN'T JUST BECAUSE I DON'T _cancelTimerChildren ON UNEQUIP, IT'S HAPPENING WHILE EQUIPPED
            while (_timeBeforeChildrenLoad >= 0)
            {
                if (_cancelLoadChildren)   //_cancelTimerChildren will stop the timer; remember to clear any values 
                {
                    _cancelLoadChildren = false;  //probably maybe cancel this with navigations that might conflict?  idk
                    break;
                }
                _timeBeforeChildrenLoad -= Time.deltaTime;
                yield return null;
            }
            //ensures input gets eaten this frame
            yield return new WaitForEndOfFrame();

            GameObject candidateFrom_NavToChild = _candidateInternal_onToChilds;
            GameObject internalConsensusGO_LCA = null;  //if this is here, it means the internalConsensusGO_LCA will already not matter.  any value initially set by OnToChild 
            int newChildIndex = 0;  //ToChild already handles this; does ToSiblings? 
            int currentChildIndex = _indexDisplayedChild;  //BECAUSE OF THE WAY CHILDREN ARE DISPLAYED & LOADED, I'M IN HELL

            if (_onToChildsBeganThisCoroutine || _childGOList == null || candidateFrom_NavToChild == null)  //<--- if this, then don't bother checking whether you're where you started; being back where you started is theoretically impossible
            {
                internalConsensusGO_LCA = candidateFrom_NavToChild;  //
                newChildIndex = 0;

                LogGoob.WriteLine($"LoadChildrenAfter ~405: set currentObject to _toChildInternalConsensusGOMaybe {candidateFrom_NavToChild} THIS WAS NOT REACHED BY THE CURRENT ERROR");
            }  //OnToSiblings REQUIRES an initial value to start at, unless you want different conditions for a post-OnToChild scroll
            else if (!_onToChildsBeganThisCoroutine)
            {
                if (originalSelGOWhenCoroutineStarted != internalConsensusGO_LCA)
                {
                    internalConsensusGO_LCA = GetPubSelection();
                    newChildIndex = currentChildIndex;  //If NavToChild has run at all, this will be 0.  if BabyCycle was erroneously running during coroutine, it may be some number beyond the index (babentimer has always been bad)
                }
                else LogGoob.WriteLine($"Stopped where we started, no need to load new child list", MessageType.Info);
            }
            else
                throw new Exception("LoadChildrenAfter wack conditions");

            _childGOList = internalConsensusGO_LCA.ListChildrenOrNull(); //This just threw a nullref; internalConsensusGO_LCA was null  //did it again
            _indexDisplayedChild = newChildIndex;  //newChildIndex exists specifically so _arbitraryChildIndex can be defined outside the brackets //ACTUALLY, ToCHILD SHOULD PROBABLY LEAVE THIS INDEX IN A MARKER STATE AND OH BOY HERE I GO LOOPING

            var firstChildAtNewChildIndex = _childGOList[newChildIndex]; //a nullref.  also this is all lagging to hell  //another nullref

            _onToChildsBeganThisCoroutine = false;
            _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", GOToStringOrElse(firstChildAtNewChildIndex), "SKIP");  //040623_1222: _childGOList[newChildIndex] got an OutOfRangeException from something //Another nullref from scrolling up fast, seems to make subsequent vertical scrolls no longer update the child list
                                                                                                                      //This has to run after either DelayLoadingOf.Children condition, so                          

            StopCoroutineStartBabies(ref timerChildrenPending);  //can confirm this corouttine wasn't stopping while the other one was, so i did this here too.

            //originalSelGOWhenCoroutineStarted shouldn't be null, because OnToChild shouldn't be able to run again if it started this timerChildrenPending, and I think NavToParent should be forbidden
            //GRAB WHICHEVER OBJECT THE OnToChild AND NavToSibling METHODS LAST SETTLED ON INTERNALLY (_toChildsToSibsInternalConsensusGO)
            //GameObject internalConsensusGO_LCA = _candidateInternal_onToChilds;
            //CHECK IF IT WAS THE SAME AS THE ORIGINAL SELECTION (AUTOMATICALLY RULE OUT POSSIBILITY IF OnToChild WAS USED SUCCESSFULLY)

            //if (_currentSelInternal_onToChild == null)  //coroutine SHOULDN'T BE RUNNING
            // yield break;
            //pull it in



            //IF  SAME AS ORIGINAL SELECTION, DO NOTHING (MAYBE REFRESH CHILD TEXT)
            //ELSE, SET FINALIZED IN STONE


            //GET NEW CHILD LIST (Won't even get here if selection was same, remember "if(){ if(optionsal){continue;} skippablestuff;} continuestuff;" exists)
            //IF CHILDLIST DOES NOT EXIST, COMMUNICATE THIS THROUGH CHILD TEXT FIELD


            //IF OnToChild WAS RUN, SIBLING LIST WILL EXIST (formerly child list, which may or may not exist anymore)
            //IF OnToChild WAS RUN, NO NEED TO GET SIBLING LIST
            //NEVER NEED TO GET SIBLING LIST ACTUALLY NVM

        }


        /// <summary>
        /// The below is NOT finishing its job, _childGOList isn't getting updated by scrolling up and down
        /// 
        /// 
        /// </summary>

        public void NavToSibling(int direction = 1)   //could probably microOptimize by splitting it up again and having different conditions using some weird hidden tags depending on whether a field was generated fresh or from prevSel, but no fuck you
        {
            List<GameObject> listOfSiblings = _siblingsOfSelGO;

            if (timerLoadingSiblings != null || listOfSiblings == null || listOfSiblings.Count <= 1)
                return;
            _babenCycleShouldRun = false;

            GameObject priorSelection = _selectedGOPublic;
            int oldNewPriorIndex = priorSelection.transform.GetSiblingIndex();
            int oldSmellySelecIndex = _selecIndex;
            if (oldSmellySelecIndex != oldNewPriorIndex)
            {
                _selecIndex = oldNewPriorIndex;
                LogGoob.Scream("NavToSibling ~510: _selectedGOPublic's SiblingIndex didn't match current stored _selecIndex, set it to match but watch it.", MessageType.Error);
            }
            //MAKE ADDITIONAL CONDITIONS FOR WHEN THE LIST IS ONLY 2
            GameObject coolNewSelection = oldNewPriorIndex.AdjacentSiblingIn(listOfSiblings, direction);
            GameObject upcomingSibling = coolNewSelection.AdjacentSiblingOfGOIn(listOfSiblings, direction);             //_selectedObject = brandSelectionGO;  //disabled on a hunch aka mercy

            SetPubSelGOFieldPlusIndexOption(coolNewSelection, true);

            string newSelectionText = GOToStringOrElse(coolNewSelection, "SKIP");

            if (timerChildrenPending == null)
                timerChildrenPending = StartCoroutine(LoadChildrenAfter(_coroutineTimerStartValueUniv, priorSelection));  //Coroutine saves the CURRENT _selectedObject to _oldSelObject; when _timerChildren runs out, will check whether _selectedObject is same as _old.  I think that means scrolling up and down with impunity wiil be fine, but again, sort the other garbage first
            else
                _timeBeforeChildrenLoad += _subsequentPressIncrementUniv;

            string upperSiblingTxt = "SKIP";  //why am i doing it with strings here instead of objects, why am i like this
            string lowerSiblingTxt = "SKIP";
            if (direction > 0)
            {
                upperSiblingTxt = $"{upcomingSibling}";
                lowerSiblingTxt = $"{priorSelection}, prevsel";
            }
            else if (direction < 0)
            {
                upperSiblingTxt = $"{priorSelection}, prevsel";
                lowerSiblingTxt = $"{upcomingSibling}";
            }
            _sgPropClass.RefreshScreen("SKIP", upperSiblingTxt, lowerSiblingTxt, "SKIP", newSelectionText);  //Refresh screen already pulls from _selectedGOPublic
        }


        public void StopCoroutineStartBabies(ref Coroutine routine)  //this runs every frame in update, probably should move the bigger pulls to things that only happen sometimes
        {
            StopCoroutine(routine);
            routine = null;

            bool loadingCoroutinesStillRunning = (timerLoadingSiblings != null || timerChildrenPending != null);
            if (loadingCoroutinesStillRunning)
            {
                throw new System.Exception("Something called StopCoroutineStartBabies while family was still loading, babies failed to start");
            }
            if (!_babenCycleShouldRun)
                _babenCycleShouldRun = true;
            else { throw new Exception("~535: _babenCycleShouldRun started out already false"); }

            bool multipleBabiesInList = (_childGOList != null && _childGOList.Count > 1);
            if (!multipleBabiesInList)  //The below line is happening constantly because GetCurrentSelection is being called constantly for the check.  icky
                return;
            else if (multipleBabiesInList)
                StartBabies();
            else if (timerLoadingSiblings != null || timerChildrenPending != null)
                StopTheBabens();
            else
                LogGoob.Scream("BABY ERROR 498: WEIRD HAPPEN", MessageType.Info);
        }
        private void StartBabies()
        {
            if (_babenCycleShouldRun == false)
            {
                _babenCycleShouldRun = true;
                if (timerBabyCycle == null)  //starts cycling through babies
                { timerBabyCycle = StartCoroutine(CycleBabens(1)); }
                else
                { LogGoob.Scream("timingBabies are already born!"); }
            }
        }

        public void CleanDeadCoroutines()
        { }



        // INSTEAD OF RUNNING ENDLESSLY AND RISKING INPUTS OVERLAPPING THE BABENCYCLE,
        // MAYBE SAVE BUTTONPRESSES IN SOME KIND OF INPUT QUEUE
        //THAT DOESN'T GET READ UNTIL THE CURRENT BABEN LOOP IS DONE???
        //and the next baben loop doesn't happen until the input queue's been read/all related coroutines are done
        public IEnumerator CycleBabens(int upDown, float time = 1f)  //should probably make sure other children exist first
        {
            if (!_babenCycleShouldRun)
            {
                LogGoob.Scream("The Babens are Revving their Engines in the Garage!!!!!");
                yield break;
            }
            GameObject currentSelection = GetGOAtSelecIndexCheck();
            string newChildText = "";

            if (_childGOList == null)
            {
                if (currentSelection.transform.childCount > 0)
                { LogGoob.WriteLine("CycleBabens: _selGO_Children is null, despite GOFromSelectionIndex having at least 1 child", MessageType.Warning); }
                else
                { LogGoob.WriteLine("CycleBabens: _selGOChildren is null, and GOFromSelectionIndex has no children"); }
                yield break;
            }
            else if (_childGOList.Count <= 1)
            {
                newChildText = $"{0.FindIndexedGOIn(_childGOList)}";
                _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", newChildText, "SKIP");
                yield break;
            }
            else for (; ; )
                {
                    if (_childGOList == null)
                    { LogGoob.WriteLine("CycleBabens: coroutine was running while _selGO_Children was null.", MessageType.Warning); }
                    else
                    {
                        List<GameObject> currentKidList = _childGOList;
                        int currentChildIndex = _indexDisplayedChild;
                        int nextChildIndex = currentChildIndex.AdjacentSibIndexIn(_childGOList, 1);  //maybe make lists a whole class component to attach to current index somehow?  idk if that's possible 
                        _indexDisplayedChild = nextChildIndex;  //the above converts an index to a GO, then this line converts it back from a GO to an index.  hell hell nightmare nightmare scream scream

                        GameObject newBaben = nextChildIndex.FindIndexedGOIn(currentKidList);
                        newChildText = newBaben.name;
                        _sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", newChildText, "SKIP");
                    }
                    yield return new WaitForSeconds(time);
                }
            //_sgPropClass.RefreshScreen("SKIP", "SKIP", "SKIP", newChildText, "SKIP");
        }

        public void StopTheBabens()
        {
            if (_babenCycleShouldRun == true || timerBabyCycle != null)
            {
                _babenCycleShouldRun = false;    //this will set _shouldBabens to false and deactivate the timingBabens coroutine when in edit mode
                StopCoroutine(timerBabyCycle);
                timerBabyCycle = null;
            }
            else
            {
                LogGoob.WriteLine("StopTheBabens L426: _babenCycleShouldRun was already false; StopoTheBabens but also wtf.");
            }
        }

        /// <summary>
        /// Raycasts until collider, gets that's attached gameobject, plugs it all into the computer
        /// </summary>
        public void EyesDrillHoles()  //SHOULD EVENTUALLY IMPLEMENT WaitBeforeLoading TO THIS, TOO
        {
            Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera
            Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
            if (hit.collider == null)
            { return; }
            else
            {
                GameObject currentSelection = _selectedGOPublic; //built-in nullcheck //never mind
                GameObject newPickedObject = hit.collider.gameObject;  //why do i have to nullcheck this too ffs //oh it's literally just when you aim at empty space ok
                Transform parentTransformOfNew = newPickedObject.transform.parent;
                if (parentTransformOfNew != null)  //can't check whether parentTransformOfNew.gameObject is null, if parentTransformOfNew ITSELF is already null, thus error.  
                {
                    newPickedObject = parentTransformOfNew.gameObject;
                    parentTransformOfNew = parentTransformOfNew.parent;
                }

                //vvv___vvv  IS _selectedObject NULL ALMOST EVER??????  vvv___vvv
                if (newPickedObject == currentSelection)  //probably make this internal, idk , trying to phase out _selectedObject in favor of index, but idk
                    return;

                LogGoob.WriteLine($"EyesDrillHoles ~670: newPickedObject is {GOToStringOrElse(newPickedObject, "NULL AUUUUGH")}");
                RefreshSGSelection(newPickedObject);     //this en
                _probeLauncherEffects.PlayLaunchClip(false);
            }
            //a new instance of this starts running fresh every time the staff fires; you were warned about this exact element of coroutines earlier yet here you are making the mistake again - surely by the second time you'd have adequate experience to know better!!!! smh fr, fr!!!!
        }
        private void RefreshSGSelection(GameObject objectToInternalSelection, string parentField = "", string siblingAbove = "", string siblingBelow = "", string childField = "")
        {

            _selectedGOPublic = objectToInternalSelection;
            Transform parentTrnsfrmOfNewInternal = objectToInternalSelection.transform.parent;

            _selecIndex = objectToInternalSelection.transform.GetSiblingIndex();   //necessary??? idfk //nullref????????
            _indexDisplayedChild = 0;
            _siblingsOfSelGO = objectToInternalSelection.GetAllSiblings();
            List<GameObject> internalChildList = _childGOList = objectToInternalSelection.ListChildrenOrNull();
            List<GameObject> currentSiblingsList = _siblingsOfSelGO;
            int selectionIndex = _selecIndex;

            //_selectedObject is a necessary fallback for when siblings haven't been loaded yet
            if (parentTrnsfrmOfNewInternal == null)        //removed ".gameObject" from this check so the code can actually check                             //This nullrefs when trying to select a parentless collider, in this case it was after having selected a different thing normally then selecting a ball
                parentField = "Fatherless";
            else
            {
                parentField = $"{parentTrnsfrmOfNewInternal.gameObject}";

                if (parentTrnsfrmOfNewInternal.childCount > 1)  //this nullrefs when selecting a parentless GO; why doesn't the nullcheck happen earlier tho
                    selectionIndex.TextFromAdjacentSiblingsIn(currentSiblingsList, out siblingAbove, out siblingBelow);
            }
            if (objectToInternalSelection.transform.childCount > 0)
                childField = $"{internalChildList[0]}";
            //nullref????
            _sgPropClass.RefreshScreen(parentField, siblingAbove, siblingBelow, childField);
        } //should probably set these differently depending

        public void ClearTerminal()
        {
            StopEditing();
            _selectedGOPublic = null;
            _childGOList = null;
            _siblingsOfSelGO = null;
            //_parentOfSelection = null;
            //_sgPropClass._sgpTxtGO_SibAboveOBSOLETE = null;  //how did this bastard nullref when it's literally not written wtf
            // _sgPropClass._sgpTxtGO_SibBelowOBSOLETE = null;
            _sgPropClass.RefreshScreen("choose...", "Select something!", "AwA", "Please select a collider", "Pick that one");
        }

        private void PlumbTheDepths()
        {
            bool otherSiblingsHaveChildren = _siblingsOfSelGO.Find(siblingsWithChildren => siblingsWithChildren.transform.childCount > 0);  // where did this even come from //i don't remember writing this line of code in Linq, but it had a typo where I was telling it to look in the Child list instead of in Siblings, no wonder it was screaming
            string warningToShriek = "Other siblings have children";
            if (!otherSiblingsHaveChildren)  //maybe subsequent ToChilds press on childless object should shrink _childGOList to only objects that have children?
                warningToShriek = "Cannot peer deeper";
            LogGoob.Scream(warningToShriek);
        }
        public void StopEditing()
        { }
        private void CopySiblingsToChildList()
        { _childGOList = _siblingsOfSelGO; }

    }

}
