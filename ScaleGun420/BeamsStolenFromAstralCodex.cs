using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{

    class BeamsAKATrails : MonoBehaviour
    {
        public static bool visible = false;

        public List<List<Transform>> targets;
        public List<List<string>> targetPaths;
        public List<LineRenderer> trails;
        Material trailMat;
        float widthMultiplier;
        
        MeshRenderer darkBrambleCloakSphereRenderer;

        private void Awake()
        { }
        public virtual void Start()
        {
            visible = false;
            //Get QM
            
            //Get trails
            trails = GetComponentsInChildren<LineRenderer>(true).ToList();
            if (trails.Count == 0) LogGoob.WriteLine("NO TRAILS FOUND", OWML.Common.MessageType.Error);
            widthMultiplier = trails[0].widthMultiplier;
            //Get targets
            targets = new List<List<Transform>>();

            //string cursorTarget = GameObject.Find("Cursor_SG")
            var cursorTargetListSolo = new List<string> { "Cursor_SG" };
            targetPaths.Add(cursorTargetListSolo);

            if (targetPaths == null) LogGoob.WriteLine("NO TARGET PATHS", OWML.Common.MessageType.Error);
            
   
            
            foreach (List<string> pathList in targetPaths)
            {
                List<Transform> pathTargets = new List<Transform>();
                foreach (string path in pathList)
                {
                    GameObject go = GameObject.Find(path);
                    if (go != null)
                        pathTargets.Add(go.transform);
                    else
                        LogGoob.WriteLine("FAILED TO FIND TRAIL TARGET " + path, OWML.Common.MessageType.Error);
                }
                targets.Add(pathTargets);
            }
            //Get material
            GameObject trailMatGO = GameObject.Find("Ship_Body/Module_Cabin/Systems_Cabin/Hatch/TractorBeam/BeamVolume/BeamParticles");
            if (trailMatGO != null)
                trailMat = trailMatGO.GetComponent<ParticleSystemRenderer>().material;
            else
                LogGoob.WriteLine("FAILED TO FIND TRAIL MATERIAL", OWML.Common.MessageType.Error);
            trailMat.color = new Color(trailMat.color.r, trailMat.color.g, trailMat.color.b, 3f);
            //Initial configuration
            for (int i = 0; i < trails.Count && i < targets.Count; i++)
            {
                trails[i].gameObject.name = targets[i][0].name;
                trails[i].material = trailMat;
            }
        }

        public virtual void Update()
        { }

        private void OldUpdate()
        {
            //Ensure targets remain accurate
            if (visible == true)
            {
                for (int i = 0; i < trails.Count && i < targets.Count; i++)  //for 
                {
                    bool validTarget = false;
                    for (int j = 0; j < targets[i].Count; j++)
                    {
                        if (targets[i][j] == null || !targets[i][j].gameObject.activeInHierarchy || (targets[i][j].transform.root.gameObject.name.Substring(0, 3) == "DB_" && darkBrambleCloakSphereRenderer.enabled == false))
                            continue;

                        trails[i].SetPosition(0, transform.position);
                        trails[i].SetPosition(3, targets[i][j].position + targets[i][j].up * 1.5f);
                        trails[i].SetPosition(1, Vector3.Lerp(trails[i].GetPosition(0), trails[i].GetPosition(3), 0.1f));
                        trails[i].SetPosition(2, Vector3.Lerp(trails[i].GetPosition(0), trails[i].GetPosition(3), 0.89f));
                        trails[i].widthMultiplier = Mathf.Min(widthMultiplier, Vector3.Distance(trails[i].GetPosition(3), transform.position) / 250);
                        validTarget = true;
                        break;
                    }
                    trails[i].gameObject.SetActive(validTarget);
                }
            }
        }
    }
}
