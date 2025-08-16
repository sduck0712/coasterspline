#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoasterSpline
{
    [CustomEditor(typeof(CoasterGenerator))]
    public class CoasterGeneratorEditor : Editor
    {
        private CoasterGenerator _coasterGenerator;
        private int selectedChainIndex = -1;
        private int selectedAnchorIndex = -1;

        private List<(int, int)> selectedOthers = new List<(int, int)>();

        private bool handleSelected = false;

        private void OnEnable()
        {
            _coasterGenerator = (CoasterGenerator)target;
        }

        private void OnSceneGUI()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Delete)
            {
                if (selectedChainIndex >= 0 && selectedAnchorIndex >= 0)
                {
                    Undo.RecordObject(_coasterGenerator, "Delete Coaster Anchor");
                    _coasterGenerator.Chains[selectedChainIndex].RemoveAnchorAt(selectedAnchorIndex);
                    selectedAnchorIndex = -1; // Reset selection after deletion

                    foreach (var (i, j) in selectedOthers)
                    {
                        _coasterGenerator.Chains[i].RemoveAnchorAt(j);
                        selectedOthers.Clear();
                    }

                    EditorUtility.SetDirty(_coasterGenerator);

                    currentEvent.Use();
                    return;
                }
            }

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.J && currentEvent.control)
            {
                if (selectedChainIndex >= 0 && selectedAnchorIndex >= 0)
                {
                    Undo.RecordObject(_coasterGenerator, "Join Coaster Anchors");

                    Vector3 pos = _coasterGenerator.Chains[selectedChainIndex].Anchors[selectedAnchorIndex].Position;
                    Vector3 handle = _coasterGenerator.Chains[selectedChainIndex].Anchors[selectedAnchorIndex].Handle;
                    float rotation = _coasterGenerator.Chains[selectedChainIndex].Anchors[selectedAnchorIndex].rotation;

                    _coasterGenerator.Chains[selectedChainIndex].SetDirty();

                    foreach (var (i, j) in selectedOthers)
                    {
                        _coasterGenerator.Chains[i].Anchors[j].Position = pos;
                        _coasterGenerator.Chains[i].Anchors[j].Handle = handle;
                        _coasterGenerator.Chains[i].Anchors[j].rotation = rotation;

                        _coasterGenerator.Chains[i].SetDirty();
                    }

                    EditorUtility.SetDirty(_coasterGenerator);

                    currentEvent.Use();
                    return;
                }
            }

            for (int i = 0; i < _coasterGenerator.Chains.Count; i++)
            {
                var chain = _coasterGenerator.Chains[i];
                for (int j = 0; j < chain.Anchors.Count; j++)
                {
                    var anchor = chain.Anchors[j];

                    if (selectedChainIndex == i && selectedAnchorIndex == j)
                    {
                        Vector3 newAnchorPosition = Handles.PositionHandle(
                            anchor.Position + _coasterGenerator.transform.position,
                            Quaternion.identity
                        );

                        if (handleSelected)
                        {
                            Vector3 handle1Pos = anchor.Position + anchor.Handle + _coasterGenerator.transform.position;
                            Vector3 newHandle1Pos = handle1Pos;
                            if (selectedAnchorIndex != chain.Anchors.Count - 1)
                                newHandle1Pos = Handles.PositionHandle(handle1Pos, Quaternion.identity);

                            Vector3 handle2Pos = anchor.Position - anchor.Handle + _coasterGenerator.transform.position;
                            Vector3 newHandle2Pos = handle2Pos;
                            if (selectedAnchorIndex != 0)
                                newHandle2Pos = Handles.PositionHandle(handle2Pos, Quaternion.identity);

                            if (GUI.changed)
                            {
                                Undo.RecordObject(_coasterGenerator, "Move Coaster Anchor");

                                Vector3 posOffset = anchor.Position - newAnchorPosition + _coasterGenerator.transform.position;

                                chain.SetDirty();

                                foreach (var (i1, j1) in selectedOthers)
                                {
                                    _coasterGenerator.Chains[i1].Anchors[j1].Position -= posOffset;
                                }



                                if (handle1Pos != newHandle1Pos)
                                {
                                    anchor.Handle = newHandle1Pos - anchor.Position - _coasterGenerator.transform.position;
                                }
                                if (handle2Pos != newHandle2Pos)
                                {
                                    anchor.Handle = -(newHandle2Pos - anchor.Position - _coasterGenerator.transform.position);
                                }
                                if (anchor.Position != newAnchorPosition - _coasterGenerator.transform.position)
                                {
                                    anchor.Position = newAnchorPosition - _coasterGenerator.transform.position;
                                }



                                EditorUtility.SetDirty(_coasterGenerator);
                            }

                            Handles.color = Color.yellow;
                            if (selectedAnchorIndex != chain.Anchors.Count - 1)
                                Handles.DrawLine(anchor.Position + _coasterGenerator.transform.position, handle1Pos);
                            if (selectedAnchorIndex != 0)
                                Handles.DrawLine(anchor.Position + _coasterGenerator.transform.position, handle2Pos);


                            Handles.color = Color.blue;
                            Handles.SphereHandleCap(0, anchor.Position + _coasterGenerator.transform.position, Quaternion.identity, 0.1f, EventType.Repaint);
                            Handles.color = Color.yellow;
                            if (selectedAnchorIndex != chain.Anchors.Count - 1)
                                Handles.SphereHandleCap(0, handle1Pos, Quaternion.identity, 0.1f, EventType.Repaint);
                            if (selectedAnchorIndex != 0)
                                Handles.SphereHandleCap(0, handle2Pos, Quaternion.identity, 0.1f, EventType.Repaint);
                        }
                    }


                    Handles.color = Color.blue;
                    if (selectedAnchorIndex == j && selectedChainIndex == i)
                    {

                        Handles.SphereHandleCap(0, anchor.Position + _coasterGenerator.transform.position, Quaternion.identity, 0.1f, EventType.Repaint);
                    }
                    else
                    {

                        float radius = 0.2f;
                        if (selectedOthers.Contains((i, j)))
                        {
                            Handles.color = new Color(233 / 255f, 100 / 255f, 41 / 255f);
                        }
                        else
                        {
                            Handles.color = Color.blue;
                            radius = 0.25f;
                        }

                        if (Handles.Button(anchor.Position + _coasterGenerator.transform.position, Quaternion.identity, radius, radius, Handles.SphereHandleCap))
                        {

                            if (currentEvent.shift)
                            {
                                selectedOthers.Add((selectedChainIndex, selectedAnchorIndex));

                                if (selectedOthers.Contains((i, j)))
                                {
                                    selectedOthers.Remove((i, j));
                                }
                            }
                            else
                            {
                                selectedOthers.Clear();
                            }

                            selectedChainIndex = i;
                            selectedAnchorIndex = j;
                            handleSelected = true;
                        }
                    }

                    if (j < chain.Anchors.Count - 1)
                    {
                        SplineAncor nextAnchor = chain.Anchors[j + 1];
                        OrientedVector newpoint = BezierCurve.GetOrientedPoint(anchor, nextAnchor, 0.5f);

                        Handles.color = Color.red;

                        Vector3 buttonPosition = newpoint.Position + _coasterGenerator.transform.position;
                        if (Handles.Button(buttonPosition, Quaternion.identity, 0.125f, 0.125f, Handles.SphereHandleCap))
                        {
                            Handles.Label(buttonPosition, "Split");

                            Undo.RecordObject(_coasterGenerator, "Add Coaster Anchor");

                            SplineAncor newAnchor = new SplineAncor
                            {
                                Position = newpoint.Position,
                                Handle = newpoint.Direction.normalized * anchor.Handle.magnitude / 2,
                                rotation = newpoint.Rotation
                            };
                            chain.InsertAnchor(j + 1, newAnchor);

                            EditorUtility.SetDirty(_coasterGenerator);
                        }
                    }
                }
            }

            if (selectedChainIndex >= 0 && selectedAnchorIndex >= 0)
            {
                ShowAnchorSettingsWindow();

                if (selectedAnchorIndex == 0 || selectedAnchorIndex == _coasterGenerator.Chains[selectedChainIndex].Anchors.Count - 1)
                {
                    List<Vector3> endPoints = new List<Vector3>();
                    foreach (var chain1 in _coasterGenerator.Chains)
                    {
                        endPoints.Add(chain1.Anchors[chain1.Anchors.Count - 1].Position);
                        endPoints.Add(chain1.Anchors[0].Position);
                    }

                    var chain = _coasterGenerator.Chains[selectedChainIndex];
                    Vector3 endpoint = chain.Anchors[selectedAnchorIndex].Position;

                    int closeChainEnd = 0;
                    foreach (var point in endPoints)
                    {
                        if (Vector3.Distance(endpoint, point) < 0.25f)
                        {
                            closeChainEnd++;
                            break;
                        }
                    }


                    if (closeChainEnd <= 1)
                    {
                        ShowChainEndMenu(chain, endpoint);
                    }
                }
            }
        }

        private void ShowChainEndMenu(SplineChain chain, Vector3 position)
        {
            Handles.BeginGUI();

            Vector3 screenspace = Camera.current.WorldToScreenPoint(position + _coasterGenerator.transform.position);
            screenspace.y = SceneView.currentDrawingSceneView.camera.pixelHeight - screenspace.y;

            GUILayout.BeginArea(new Rect(screenspace.x - 50, screenspace.y - 150, 100, 75), "Endpoint Menu", GUI.skin.window);

            int index = 0;
            if (position == chain.Anchors[chain.Anchors.Count - 1].Position)
            {
                index = chain.Anchors.Count - 1;
            }

            if (GUILayout.Button("Add Segment"))
            {
                Undo.RecordObject(_coasterGenerator, "Add Coaster Anchor");
                SplineAncor lastAnchor = chain.Anchors[index];
                SplineAncor newAnchor = new SplineAncor
                {
                    Position = lastAnchor.Position + lastAnchor.Handle * 2,
                    Handle = lastAnchor.Handle,
                    rotation = lastAnchor.rotation
                };

                if (index == chain.Anchors.Count - 1)
                {
                    chain.InsertAnchor(chain.Anchors.Count, newAnchor);
                }
                else
                {
                    newAnchor.Position = lastAnchor.Position - lastAnchor.Handle * 2;
                    chain.InsertAnchor(0, newAnchor);
                }
                chain.UpdateLengthCache();
                EditorUtility.SetDirty(_coasterGenerator);
            }

            if (GUILayout.Button("Add Chain"))
            {
                Undo.RecordObject(_coasterGenerator, "Add Coaster Chain");
                SplineAncor lastAnchor = chain.Anchors[chain.Anchors.Count - 1];
                SplineChain newChain = new SplineChain
                {
                    Anchors = new List<SplineAncor>
                {
                    new SplineAncor
                    {
                        Position = lastAnchor.Position,
                        Handle = lastAnchor.Handle,
                        rotation = lastAnchor.rotation
                    },
                    new SplineAncor
                    {
                        Position = lastAnchor.Position + lastAnchor.Handle*2,
                        Handle = lastAnchor.Handle,
                        rotation = lastAnchor.rotation
                    }
                }
                };
                _coasterGenerator.Chains.Add(newChain);
                EditorUtility.SetDirty(_coasterGenerator);
            }

            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private void ShowAnchorSettingsWindow()
        {
            var chain = _coasterGenerator.Chains[selectedChainIndex];
            var anchor = chain.Anchors[selectedAnchorIndex];
            var prevAnchor = selectedAnchorIndex > 0 ? chain.Anchors[selectedAnchorIndex - 1] : chain.Anchors[selectedAnchorIndex + 1];

            Handles.BeginGUI();

            Vector3 screenspace = Camera.current.WorldToScreenPoint(anchor.Position + _coasterGenerator.transform.position);
            screenspace.y = SceneView.currentDrawingSceneView.camera.pixelHeight - screenspace.y;

            GUILayout.BeginArea(new Rect(screenspace.x - 100, screenspace.y + 100, 200, 50), selectedAnchorIndex + " - Banking: " + anchor.rotation, GUI.skin.window);

            anchor.rotation = GUILayout.HorizontalSlider(anchor.rotation, prevAnchor.rotation - 360f, prevAnchor.rotation + 360f);

            foreach (var (i, j) in selectedOthers)
            {
                _coasterGenerator.Chains[i].Anchors[j].rotation = anchor.rotation;
            }

            GUILayout.EndArea();

            Handles.EndGUI();

            if (GUI.changed)
            {
                Undo.RecordObject(_coasterGenerator, "Modify Coaster Anchor");
                EditorUtility.SetDirty(_coasterGenerator);
            }
        }
    }
}

#endif