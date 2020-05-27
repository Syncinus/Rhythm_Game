using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VectorGraphics;

[ExecuteInEditMode]
public class VectorGraphicsTest : MonoBehaviour
{
    public Transform[] controlPoints;

    private Scene m_Scene;
    private Shape m_Path;
    private VectorUtils.TessellationOptions m_Options;
    private Mesh m_Mesh;

    void Start()
    {
        // Prepare the vector path, add it to the vector scene.
        /*m_Path = new Shape()
        {
            Contours = new BezierContour[] { new BezierContour() { Segments = new BezierPathSegment[2] } },
            PathProps = new PathProperties()
            {
                Stroke = new Stroke() { Color = Color.white, HalfThickness = 0.1f }
            }
        };*/
        m_Path = new Shape()
        {
            Contours = new BezierContour[] { new BezierContour() { Segments = new BezierPathSegment[2] } }
        };
        //VectorUtils.MakeCircleShape(m_Path, Vector2.zero, 5.0f);
        m_Path.Fill = new SolidFill() { Color = Color.blue };
        m_Path.PathProps = new PathProperties()
        {
            Stroke = new Stroke() { Color = Color.red }
        };

        m_Scene = new Scene()
        {
            Root = new SceneNode() { Shapes = new List<Shape> { m_Path } }
        };

        m_Options = new VectorUtils.TessellationOptions()
        {
            StepDistance = 1.0f,
            MaxCordDeviation = float.MaxValue,
            MaxTanAngleDeviation = Mathf.PI / 2.0f,
            SamplingStepSize = 0.01f
        };

        // Instantiate a new mesh, it will be filled with data in Update()
        m_Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = m_Mesh;
    }

    void Update()
    {
        if (m_Path == null || m_Scene == null)
        {
            Start();
        }

        Debug.Log(m_Path);
        m_Path.PathProps.Stroke.HalfThickness = 0.5f;
        m_Path.Contours[0].Segments = VectorUtils.MakeArc(Vector2.zero, 0f, Mathf.PI * 2, 10f);

        // Tessellate the vector scene, and fill the mesh with the resulting geometry.
        var geoms = VectorUtils.TessellateScene(m_Scene, m_Options);
        VectorUtils.FillMesh(m_Mesh, geoms, 1.0f);
    }
}