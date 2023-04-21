using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ScaleGun420
{
    public class StaffSpawner : MonoBehaviour
    {
        public GameObject _sgCamHoldTransformGO;
        public GameObject _sgBodyHoldTransformGO;
        public GameObject _sgBodyStowTransformGO;

        public GameObject _GO_sgTool;  //MUST BE PUBLIC  //Must it?
        public GameObject _socketScalegunTorso;
        public GameObject _socketScalegunPlayerCam;

        public GameObject propGroupGO_SG;
        public GameObject _beamsJohnson;
        private GameObject _theCursor;


        public GameObject GO_THCanvas;
        public Canvas _thCanvas;
        public Text _text_Selection;
        public Text _text_Parent;

        public GameObject GOtxt_SibAbove;
        public Text _text_SibAbove;
        public GameObject GOtxt_SibBelow;
        public Text _text_SibBelow;

        public GameObject _huskNOMCanv;
        public Canvas _canvasCpntNOM;
        public GameObject huskTxt_Child;
        public Text _textCpnt_Child;

        public GameObject _sgPlaceholderGO;

        private RectTransform _mainTextRecTra;
        private HorizontalWrapMode _horizontalOverflow = HorizontalWrapMode.Overflow;
        private Vector2 _textSizeDelta = new Vector2(1400, 35);
        private float siblingAlignment = -835;


        //public ScalegunToolClass _theGunToolClass;


        //You attached the components to the shell instead of the prop proper, dingus

        public void SpawnEverything()
        {
            ConfigureHoldTransforms();

            //_socketScalegunTorso = Locator.GetPlayerBody().transform.CreateChild("SocketScalegunTorso", false);  //031623_0653: spawns an inactive empty SGToolGO as a child of the player. //SETTING TO TRUE BECAUSE FUCK IT I GUESS

            _GO_sgTool = Locator.GetPlayerBody().transform.CreateChild("ScalegunHusk", false);

            propGroupGO_SG = _GO_sgTool.transform.InstantiatePrefab("brittlehollow/meshes/props", "BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
            "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff", true, new Vector3(0, -0.9f, -0.0005f), new Vector3(0, 180, 0));  //040723_1811: Set to spawn active because idk, everything's broken now
            propGroupGO_SG.name = "ScalegunGroup";

            SpawnBeamOrigin();



            var placeholderGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholderGO.name = "Placeholder_SG";
            placeholderGO.GetComponentInChildren<BoxCollider>().enabled = false;
            //var brother = placeholderGO.GetCollisionGroup()._colliders ;
            placeholderGO.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            placeholderGO.transform.parent = _GO_sgTool.transform;
            placeholderGO.transform.localPosition = _GO_sgTool.transform.localPosition;
            placeholderGO.transform.localEulerAngles = _GO_sgTool.transform.localEulerAngles;
            //placeholderGO.SetActive(false);


            _theCursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _theCursor.name = "Cursor_SG";
            _theCursor.GetComponentInChildren<BoxCollider>().enabled = false;
            //var brother = placeholderGO.GetCollisionGroup()._colliders ;
            _theCursor.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            _theCursor.transform.parent = _GO_sgTool.transform;
            _theCursor.transform.localPosition = _GO_sgTool.transform.localPosition;
            _theCursor.transform.localEulerAngles = _GO_sgTool.transform.localEulerAngles;
            _theCursor.AddComponent<BeamsAKATrails>();
            //_theCursor.AddComponent<PlayerLockOnTargeting>();
            //cursorGO.SetActive(false);

            DupeTranslatorCanvasGO();
            MangleTranslator();  //oh you forgot to put the method in SpawnEverything, ok //and consider putting it AFTER the thing that spawns the translator dupe dummy
            StealGOsForSiblingLabels();


            SpawnTextCanvas(propGroupGO_SG.transform, "SG_NOMCanvas", "SG_Babens");
            ///HomebrewGOForNOMCanvas();
            ///

            //wack ass
            _GO_sgTool.AddComponent<ScalegunToolClass>();  //hopefully the host _sgtool_GO's inactivity prevents its new ScalegunTool pilot from waking up, or it'll reach for ScalegunPropClass too early
            _GO_sgTool.AddComponent<ScalegunAnimationSuite>();
            _GO_sgTool.AddComponent<ScalegunPropClass>(); //ScalegunTool declares a PropClass; hopefully not 2late to attach & designate it to the _sgPropGroupject.
            _GO_sgTool.AddComponent<SgNavComputer>();
            _GO_sgTool.AddComponent<TheEditMode>();

            //_GO_sgTool.SetActive(true);
            _GO_sgTool.SetActive(true);
            //   _thCanvas.enabled = false; //031823_0614: doing this since TranslatorProp did it but it wasn't here yet //update: nope //031823_1524: Sudden unexpected nullref?
            // _canvasCpntNOM.enabled = false;
            //_sgOwnPropGroupject = ScaleGun420Modbehavior.Instance._ //Might have to define it here.  How do I break the chains?

            //_theProp.SetActive(true);  //what NomaiTranslatorProp does, but better-labeled.  TranslatorProp sets its whole parent propgroup inactive at end of its Awake (the parts of it relevant to me) }

            //_theGunToolClass.enabled = true; //031823_0622: put back after the other one in hopes of addressing a first-time-equip bug  UPDATE: THAT DID NOTHING EITHER  
            SpawnLineRenderers();

            LogGoob.WriteLine("idfk m8 it should be active", OWML.Common.MessageType.Success);
        }
        private void ConfigureHoldTransforms()  //does all the object spawning/hierarchies that the base game's creators probably handled better in unity.  idfk.  Does things in such 
        {
            _sgCamHoldTransformGO = Locator.GetPlayerCamera().gameObject.transform.CreateChild("SG_HoldTransform_CAMERA", true, new Vector3(0.14f, -0.425f, 0.11f), new Vector3(19, 5, 8));
            _sgBodyHoldTransformGO = Locator.GetPlayerTransform().CreateChild("SG_HoldTransform_BODY", true, new Vector3(0.4f, -0.25f, 0.5f), new Vector3(10, 10, 5));

        }


        //_sgBodyStowTransformGO = Locator.GetPlayerBody().transform.CreateChild("SgBodyStowTransform_husk", true);  //are these redundant?  am i usin em at all



        private void DupeTranslatorCanvasGO()
        {
            GO_THCanvas = Instantiate(GameObject.Find("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas"), propGroupGO_SG.transform);

            GO_THCanvas.name = "SG_THCanvas";

            GO_THCanvas.transform.localEulerAngles = new Vector3(25f, 160f, 350f);
            GO_THCanvas.transform.localPosition = new Vector3(0.15f, 1.75f, 0.05f);
            GO_THCanvas.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);

            _mainTextRecTra = propGroupGO_SG.transform.GetComponentInChildren<RectTransform>(true); //031823_0523: swapped to before _sgpTextFieldMain gets defined, idk why //040723_1516: What IS a RectTransform, anyway, and which one does this grab?
            _mainTextRecTra.pivot = new Vector2(1f, 0.5f);  //idfk brother, i think it's setting them correctly

            _thCanvas = GO_THCanvas.transform.GetComponentInChildren<Canvas>(true);

            GO_THCanvas.SetActive(true);  //031823_0616: This is a definite "true" moment (don't change)
        }                                     //_sgpGO_THCanvas = base.transform.GetComponentInChildren<Canvas>(true);  //031823_0627: GETTING RID OF THE (true) MAYBE?   //031923_1831: never found out whether that would work because VS broke

        private void MangleTranslator()
        {
            _text_Selection = GO_THCanvas.transform.GetChildComponentByName<Text>("TranslatorText"); //.GetComponent<Text>();  //why are YOU nullref'ing wtf
            _text_Selection.name = "SG_Selection";
            _text_Selection.rectTransform.localPosition = new Vector2(1100, 260);
            _text_Selection.rectTransform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            _text_Selection.alignment = TextAnchor.MiddleCenter;
            //_text_Selection.enabled = true; //needless, already spawns active

            _text_Parent = GO_THCanvas.transform.GetChildComponentByName<Text>("PageNumberText"); //.GetComponent<Text>();   //THIS IS ALL YOU NEED TO SPAWN NEW LASSES YOU DINGUS
            _text_Parent.name = "SG_Parent";
            _text_Parent.rectTransform.localPosition = new Vector2(-1680, 245);
            _text_Parent.rectTransform.sizeDelta = _textSizeDelta;
            _text_Parent.horizontalOverflow = _horizontalOverflow;

            _text_Parent.enabled = true;  //031823_0608: setting to false doesn't fix the thing, and just leaves it disabled. //032623_1921: idk why this is still here but I'll leave it for now.
        }

        private void StealGOsForSiblingLabels()
        {
            GOtxt_SibAbove = GO_THCanvas.transform.InstantiateTextGO_FromPathForSomeReason("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "SG_SiblingUp",
                out _text_SibAbove, new Vector2(siblingAlignment, 140), _textSizeDelta, _horizontalOverflow);

            GOtxt_SibBelow = GO_THCanvas.transform.InstantiateTextGO_FromPathForSomeReason("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "SG_SiblingDown",
                out _text_SibBelow, new Vector2(siblingAlignment, 0), _textSizeDelta, _horizontalOverflow);
        }

        public void SpawnTextCanvas(Transform asChildOf, string nameCanvasGO_spawner, string nameTextGO_spawner)
        {
            Transform attachTransform_internal = propGroupGO_SG.transform;
            if (asChildOf != null && asChildOf.gameObject != null)
                attachTransform_internal = asChildOf;


            Vector2 canvRTSizeDelta = new Vector2(1400, 200);
            float goCanvScale = 0.0004f;
            Vector3 goCanvLocalPosition = new Vector3(0, 1.7f, 0.15f);
            Vector3 goCanvLocalEulers = new Vector3(45, 180, 0);

            Canvas canvas = attachTransform_internal.transform.AddCanvasGO(
                nameCanvasGO_spawner, canvRTSizeDelta, goCanvScale, goCanvLocalPosition, goCanvLocalEulers);


            Vector2 txt_rectSizeDelta = new Vector2(500, 500);
            int fontSize = 40;
            string spaceFontName = "fonts/english - latin/SpaceMono-Regular";

            Text text = canvas.transform.AddTextGO(
                nameTextGO_spawner, txt_rectSizeDelta, fontSize, spaceFontName);
        }
        /// <summary>
        /// Notes, discoveries, and recommended ingredient measurements:
        /// 
        ///   \__ canvHusk __/
        /// -=- RectTransform -=-
        ///  .localScale = 0.00004
        ///  .sizeDelta =  1000 x 200
        ///  
        /// 
        ///   \___ txtHusk ___/
        /// -=- RectTransform -=-
        ///   .sizeDelta = 500 x 500 (pending the razor)
        ///   
        ///   -=-  TextCpnt: -=-
        ///   .fontSize  40
        ///   
        /// 
        /// </summary>
        private void HomebrewGOForNOMCanvas()
        {
            _huskNOMCanv = propGroupGO_SG.GivesBirthTo("SG_NOMCanvas", true, new Vector3(0, 1.7f, 0.15f), new Vector3(45, 180, 0), 0.003f);  //For some reason, spawns with a text component visible from the main GameObject, idk why

            _canvasCpntNOM = _huskNOMCanv.AddComponent<Canvas>();  //rectTransform seems to come prepackaged for some reason idfk
            _canvasCpntNOM.worldCamera = Locator.GetPlayerCamera().mainCamera;  //Check when canvases are set, and you'll find these values as the only ones being set
            _canvasCpntNOM.renderMode = RenderMode.WorldSpace;

            huskTxt_Child = _huskNOMCanv.GivesBirthTo("SG_Babens", true);   //Get the TextGenerator? idk it's in the instantiated stuff but not in Text by default
            _textCpnt_Child = huskTxt_Child.AddComponent<Text>();

            huskTxt_Child.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 500);

            Font spaceFont = new Font("SpaceMono-Regular");

            //RandomizeFontForSomeReasonSpawner();
            huskTxt_Child.AddComponent<TypeEffectText>(); //is this a whole text component in and of itself?
            //_sgpTxtGO_Child = _sgpGO_NOMCanvas.transform.InstantiateTextGO_FromPathForSomeReason("Player_Body/PlayerCamera/NomaiTranslatorProp/TranslatorGroup/Canvas/PageNumberText", "Children",
            // out _sgpTxt_Child, new Vector2(siblingAlignment + 900, 75), textSizeDelta, horizontalOverflow);
            //
        }
        private void RandomizeFontForSomeReasonSpawner()
        {
            var fontList = Font.GetOSInstalledFontNames();
            int fontIndex = UnityEngine.Random.Range(0, (fontList.Count()));
            _textCpnt_Child.font = UnityEngine.Font.CreateDynamicFontFromOSFont(fontList[fontIndex].ToString(), 100);  //idk what the size parameter does; i've set it to 1 and to 100 and there's no noticeable difference; maybe it's instantly getting overwritten by something?  idk
        }

        private void SpawnBeamOrigin()
        {
            _beamsJohnson = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _beamsJohnson.name = "BeamOrigin_SG";
            _beamsJohnson.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            _beamsJohnson.transform.parent = propGroupGO_SG.transform;
            _beamsJohnson.transform.localPosition = new Vector3(0f, 2f, 0.0005f);
            _beamsJohnson.transform.localEulerAngles = default;

        }
        private void SpawnLineRenderers()
        {

            GameObject beamMatGO = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/Hatch/TractorBeam/BeamVolume/BeamParticles");
            if (beamMatGO == null)
                return;
            var beamMat = beamMatGO.GetComponent<ParticleSystemRenderer>().material;

            for (int i = 0; i < 6; i++)
            {
                string beamName = $"SGBeam{i}";
                GameObject beamObject = _theCursor.GivesBirthTo(beamName, true);

                LineRenderer lineRenderer = beamObject.AddComponent<LineRenderer>();
                lineRenderer.startColor = Color.blue;
                lineRenderer.startWidth = 1;
                lineRenderer.SetPosition(0, _theCursor.transform.position);
                lineRenderer.SetPosition(1, _beamsJohnson.transform.position);
                lineRenderer.material = beamMat;  //value cannot be null
            }

        }
    }
}
