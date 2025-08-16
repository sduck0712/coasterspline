using System.Collections.Generic;
using UnityEngine;

namespace CoasterSpline
{
    [RequireComponent(typeof(CoasterGenerator))]
    public class CoasterMesh : MonoBehaviour
    {
        private CoasterGenerator coasterGenerator;

        public Mesh[] tracks;
        public CoasterSupport[] supports;

        private GameObject trackObject;
        private MeshFilter trackMeshFilter;
        private MeshRenderer trackMeshRenderer;
        [SerializeField] private Material trackMaterial;

        private GameObject footerObject;
        [SerializeField] private GameObject FooterFrefab;

        public List<SplineChain> prevChains = new List<SplineChain>();

        private bool _sameAsLastFrame = false;
        private bool _changed = false;

        private void Awake()
        {
            coasterGenerator = GetComponent<CoasterGenerator>();

            GenerateMesh(tracks);
        }

        private int _changedChainIndex = -1;

        private void Update()
        {
            _sameAsLastFrame = true;

            if (prevChains.Count != coasterGenerator.Chains.Count)
            {
                _changed = true;
                _sameAsLastFrame = false;
                _changedChainIndex = 0;
            }
            else
            {
                for (int i = 0; i < prevChains.Count; i++)
                {
                    SplineChain prevChain = prevChains[i];
                    SplineChain chain = coasterGenerator.Chains[i];
                    if (prevChain.Anchors.Count != chain.Anchors.Count)
                    {
                        _changed = true;
                        _sameAsLastFrame = false;
                        _changedChainIndex = i;
                        break;
                    }
                    for (int j = 0; j < prevChain.Anchors.Count; j++)
                    {
                        SplineAncor prevAnchor = prevChain.Anchors[j];
                        SplineAncor anchor = chain.Anchors[j];
                        if (prevAnchor.Position != anchor.Position || prevAnchor.Handle != anchor.Handle || prevAnchor.rotation != anchor.rotation)
                        {
                            _changed = true;
                            _sameAsLastFrame = false;
                            _changedChainIndex = i;
                            break;
                        }
                    }
                    if (_changed) break;
                }
            }

            if (_changed)
            {
                prevChains.Clear();
                foreach (SplineChain chain in coasterGenerator.Chains)
                {
                    SplineChain newChain = new SplineChain();
                    foreach (SplineAncor anchor in chain.Anchors)
                    {
                        SplineAncor newAnchor = new SplineAncor();
                        newAnchor.Position = anchor.Position;
                        newAnchor.Handle = anchor.Handle;
                        newAnchor.rotation = anchor.rotation;
                        newChain.Anchors.Add(newAnchor);
                    }
                    prevChains.Add(newChain);
                }
            }

            if (_changed && _sameAsLastFrame)
            {
                _changed = false;

                foreach (Transform child in transform)
                {
                    if (child.GetComponent<CoasterSupport>() != null)
                    {
                        Destroy(child.gameObject);
                    }
                }

                foreach (SplineChain chain in coasterGenerator.Chains)
                {

                    if (chain != null)
                    {
                        UpdateMesh(tracks, chain);
                    }

                    OrientedVector[] supportOrigins = coasterGenerator.getSupportLocations(chain);

                    for (int i = 0; i < supportOrigins.Length; i++)
                    {
                        OrientedVector supportOrigin = supportOrigins[i];

                        if (supportOrigin.Up != Vector3.up)
                        {
                            if (Mathf.Abs(supportOrigin.Direction.normalized.y) > 0.9f)
                            {
                                continue;
                            }
                        }

                        float bestFit = float.MaxValue;
                        CoasterSupport bestSupport = null;
                        foreach (CoasterSupport s in supports)
                        {
                            float rotation = supportOrigin.Rotation;
                            if (supportOrigin.Up != Vector3.up)
                            {
                                rotation -= supportOrigin.DeltaRotation;
                            }
                            float fit = Mathf.Abs(Mathf.DeltaAngle(rotation, s.Rotation));
                            if (fit < bestFit)
                            {
                                bestFit = fit;
                                bestSupport = s;
                            }
                        }

                        GameObject supportObject = Instantiate(bestSupport.gameObject);
                        supportObject.transform.parent = transform;
                        supportObject.transform.position = supportOrigin.Position + transform.position;
                        Vector3 direction = supportOrigin.Direction;
                        direction.y = 0;
                        supportObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

                        CoasterSupport support = supportObject.GetComponent<CoasterSupport>();

                        bool intersected = false;
                        foreach (SplineChain chain1 in coasterGenerator.Chains)
                        {
                            if (support.Intersects(0, 0.5f, chain1, coasterGenerator.transform.position))
                            {
                                intersected = true;
                            }
                        }

                        if (!intersected)
                        {
                            support.ExtendLegs(0, FooterFrefab);
                        }
                        else
                        {
                            Destroy(supportObject);
                        }
                    }
                }
            }
        }


        public void UpdateMesh(Mesh[] track, SplineChain chain)
        {
            if (chain.MeshGO != null)
            {
                MeshRenderer meshRenderer = chain.MeshGO.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = chain.MeshGO.GetComponent<MeshFilter>();

                meshFilter.mesh = coasterGenerator.GenerateSubMesh(chain, track[0], false);
            }
        }

        public void GenerateMesh(Mesh[] track)
        {


            foreach (SplineChain chain in coasterGenerator.Chains)
            {
                trackObject = new GameObject("Track_");
                trackObject.transform.parent = transform;
                trackObject.transform.position = transform.position;

                trackMeshFilter = trackObject.AddComponent<MeshFilter>();
                trackMeshFilter.mesh = new Mesh();

                trackMeshRenderer = trackObject.AddComponent<MeshRenderer>();
                trackMeshRenderer.material = trackMaterial;

                trackMeshFilter.mesh = coasterGenerator.GenerateSubMesh(chain, track[0], false);
                trackMeshFilter.mesh.MarkDynamic();

                chain.MeshGO = trackObject;
            }

            foreach (SplineChain chain in coasterGenerator.Chains)
            {
                OrientedVector[] supportOrigins = coasterGenerator.getSupportLocations(chain);

                for (int i = 0; i < supportOrigins.Length; i++)
                {
                    OrientedVector supportOrigin = supportOrigins[i];

                    if (supportOrigin.Up != Vector3.up)
                    {
                        if (Mathf.Abs(supportOrigin.Direction.normalized.y) > 0.9f)
                        {
                            continue;
                        }
                    }
                    float bestFit = float.MaxValue;
                    CoasterSupport bestSupport = null;
                    foreach (CoasterSupport s in supports)
                    {
                        float rotation = supportOrigin.Rotation;
                        if (supportOrigin.Up != Vector3.up)
                        {
                            rotation -= supportOrigin.DeltaRotation;
                        }
                        float fit = Mathf.Abs(Mathf.DeltaAngle(rotation, s.Rotation));
                        if (fit < bestFit)
                        {
                            bestFit = fit;
                            bestSupport = s;
                        }
                    }

                    GameObject supportObject = Instantiate(bestSupport.gameObject);
                    supportObject.transform.parent = transform;
                    supportObject.transform.position = supportOrigin.Position + transform.position;
                    Vector3 direction = supportOrigin.Direction;
                    direction.y = 0;
                    supportObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

                    CoasterSupport support = supportObject.GetComponent<CoasterSupport>();

                    bool intersected = false;
                    foreach (SplineChain chain1 in coasterGenerator.Chains)
                    {
                        if (support.Intersects(0, 0.5f, chain1, coasterGenerator.transform.position))
                        {
                            intersected = true;
                        }
                    }

                    if (!intersected)
                    {
                        support.ExtendLegs(0, FooterFrefab);
                    }
                    else
                    {
                        Destroy(supportObject);
                    }
                }
            }

        }
    }
}
