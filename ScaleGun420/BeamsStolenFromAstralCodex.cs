using Steamworks;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace ScaleGun420
{
    /// <summary>
    /// USE Mesh.Bounds 
    /// </summary>
    class BeamsAKATrails : MonoBehaviour
    {
        //private static bool visible = false;

        private GameObject _cursorSG;
        private List<Vector3> cursorCorners;
        public List<List<string>> targetPaths;
        public List<LineRenderer> trails;
        Material trailMat;
        float widthMultiplier;
        private Transform _sgCursorTransform;
        private Transform _sgBeamOriginTransform;
        private GameObject _sgBeamOrigin;
        private float updateTimerTick = 0f;

        private const float N = -0.5f;
        private const float P = 0.5f;

        MeshRenderer darkBrambleCloakSphereRenderer;

        private void Awake()
        {
            _cursorSG = GameObject.Find("Cursor_SG");
            _sgBeamOrigin = GameObject.Find("BeamOrigin_SG");
            _sgCursorTransform = _cursorSG.transform;
            _sgBeamOriginTransform = transform.GetChildComponentByName<Transform>("BeamOrigin_SG");

            //below was taken from Start, idk if this will help  //042223_2016 Nope that didn't work either, idfk chief. <--_2010: Yeah now we're back to the beams not rendering, let's try just defining trails

            //trails = GetComponentsInChildren<LineRenderer>(true).ToList();


        }

        private enum CornerTransform  //could probably do this more cleanly with some movesprings 
        {
            UpBackLeft = 0,
            UpForeLeft = 1,
            UpForeRight = 2,
            DownForeRight = 3,
            DownBackRight = 4,
            DownBackLeft = 5,
        }

        public virtual void Start()  //guess the nullref in SetBeamsActiveSG on startup/first Prop.FinishEquipAnimation means this Start hasn't run yet, otherwise trails wouldn't be null then
        {
            trails = GetComponentsInChildren<LineRenderer>(true).ToList();
            cursorCorners = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            { cursorCorners.Add(new Vector3(0, 0, 0)); }
            widthMultiplier = 0.5f;
            gameObject.SetActive(false);   //  WORKS!!!! 
        }
        private void RecommendedVectors()
        {
            Vector3 Start = transform.position;
            Vector3 End = transform.worldToLocalMatrix.MultiplyPoint(_sgCursorTransform.position);
        }
        public void SetBeamsActiveSG(bool activateBeams)  //should probably call this in EditMode instead, leaving editmode should disable these
        {
            this.enabled = activateBeams;
            gameObject.SetActive(activateBeams);  // nope actually this works almost perfectly aside from starting visible //idk if this will do the thing, it might just deactivate the cursor and any connected gameObject and cause the whole game to collapse
            ///for (int i = 0; i < trails.Count; i++)    //nullref on start , because 
            /// trails[i].enabled = activateBeams;
        }

        private void EnumeratedSetCorners(Transform theOrigin, int theBeamInt)  //this is illegibile but it does the joj
        {
            float xOff;
            float yOff;
            float zOff;

            if (theBeamInt == 0 || theBeamInt == 1 || theBeamInt == 5)
                xOff = P;
            else xOff = N;

            if (theBeamInt == 0 || theBeamInt == 1 || theBeamInt == 2)
                yOff = P;
            else yOff = N;

            if (theBeamInt == 1 || theBeamInt == 2 || theBeamInt == 3)
                zOff = P;
            else zOff = N;

            cursorCorners[theBeamInt] = theOrigin.TransformPoint(xOff, yOff, zOff);
        }

        private void OffsetCornersTransformPoints(GameObject theTarget)
        {
            Transform theOrigin = theTarget.transform;

            cursorCorners[0] = theOrigin.TransformPoint(P, P, N);
            cursorCorners[1] = theOrigin.TransformPoint(P, P, P);
            cursorCorners[2] = theOrigin.TransformPoint(N, P, P);
            cursorCorners[3] = theOrigin.TransformPoint(N, N, P);
            cursorCorners[4] = theOrigin.TransformPoint(N, N, N);
            cursorCorners[5] = theOrigin.TransformPoint(P, N, N);
        }

        void GetBoundsOf()
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
            Bounds bounds = mesh.bounds;
            int i = 0;
            while (i < uvs.Length)
            {
                uvs[i] = new Vector2(vertices[i].x / bounds.size.x, vertices[i].z / bounds.size.x);
                i++;
            }
            mesh.uv = uvs;
        }
        private void Update()
        {
            if (_cursorSG.transform == null) //not running augh
                return;
            //if (updateTimerTick < 10f)
            //    updateTimerTick += 0.1f;
            // else
            // {
            //     LogGoob.WriteLine("Beams Update ticking nicely", OWML.Common.MessageType.Info);
            //     updateTimerTick = 0f;
            //  }
            //OffsetCornersTransformPoints(_cursorSG);
            Transform currentCursorLocation = _cursorSG.transform;
            for (int i = 0; i < trails.Count; i++)
            {
                EnumeratedSetCorners(currentCursorLocation, i);
                trails[i].SetPosition(0, cursorCorners[i]);//
                trails[i].SetPosition(1, _sgBeamOrigin.transform.position);
                trails[i].widthMultiplier = Mathf.Min(widthMultiplier, Vector3.Distance(trails[i].GetPosition(1), transform.position) / 250);
            }
        }

        private Vector3 MakeVector(Vector3 theMonster, float X, float Y, float Z)
        {
            return new Vector3(theMonster.x + X, theMonster.y + Y, theMonster.z + Z);
        }




        private void SetTheWretchedValues()
        {
            Vector3 theBeast = _sgBeamOrigin.transform.worldToLocalMatrix.MultiplyPoint(_cursorSG.transform.position); //_cursorSG.transform.localToWorldMatrix.MultiplyPoint(Vector3.one); 

            cursorCorners[0] = MakeVector(theBeast, P, P, N);  //just use TransformPoint
            cursorCorners[1] = MakeVector(theBeast, P, P, P);
            cursorCorners[2] = MakeVector(theBeast, N, P, P);
            cursorCorners[3] = MakeVector(theBeast, N, N, P);
            cursorCorners[4] = MakeVector(theBeast, N, N, N);
            cursorCorners[5] = MakeVector(theBeast, P, N, N);
        }

        private void FuxedUpdateShhh()
        {
            if (_cursorSG.transform == null)
                return;
            SetTheWretchedValues();
            foreach (LineRenderer trail in trails)
                trail.SetPosition(0, transform.position);

            int randomIndex = UnityEngine.Random.Range(0, trails.Count);
            var booty = trails[randomIndex];


            booty.SetPosition(1, cursorCorners[randomIndex]);
            booty.widthMultiplier = Mathf.Min(widthMultiplier, Vector3.Distance(booty.GetPosition(1), transform.position) / 250);
        }






        private void WeirdUpdate()
        {
            //Calculate new postion 
            ///  Vector3 newBeginPos = transform.localToWorldMatrix * new Vector4(beginPos.x, beginPos.y, beginPos.z, 1);
            /// Vector3 newEndPos = transform.localToWorldMatrix * new Vector4(endPos.x, endPos.y, endPos.z, 1);

            //Apply new position
            /// diagLine.SetPosition(0, newBeginPos);
            /// diagLine.SetPosition(1, newEndPos);
        }
    }
}
