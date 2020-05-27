using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Linq;
using System.IO;
using UnityEngine;
using LevelData;
using org.mariuszgromada.math.mxparser;
using System.ComponentModel;
using UnityEngine.Networking;
using Unity.VectorGraphics;
using org.mariuszgromada.math.mxparser.mathcollection;

// TODO:
// Make Vector2 and Color capable of math and being dynamic properties
// Implement better error handling for audio loading and stuff
// Cleanup the level functio system (add FunctionParmeter and FunctionParemeters as types)

public interface ILevelParsable
{
    void Parse(string Info, Dictionary<string, string> Variables, ref bool IsDynamic);
}

public readonly struct LevelFunction : IComparable
{
    public readonly string Name;
    public readonly float Time;
    public readonly Dictionary<string, object> Properties;
    public readonly Dictionary<string, Type> DynamicFlags;
    public readonly Dictionary<string, string> ComplierVariables;

    public LevelFunction(string Name_, float Time_, Dictionary<string, object> Properties_, Dictionary<string, Type> DynamicFlags_, Dictionary<string, string> ComplierVariables_)
    {
        Name = Name_;
        Time = Time_;
        Properties = Properties_;
        DynamicFlags = DynamicFlags_;
        ComplierVariables = ComplierVariables_;
    }

    public int CompareTo(object other)
    {
        LevelFunction OtherFunction = (LevelFunction)other;
        return Time.CompareTo(OtherFunction.Time);
    }
};

public class Level
{
    public string Name;
    public string Creator;
    public string Song;
    public Queue<LevelFunction> Map;

    public Level(string Name_, string Creator_, string Song_, Queue<LevelFunction> Map_)
    {
        Name = Name_;
        Creator = Creator_;
        Song = Song_;
        Map = Map_;
    }
};

public class LevelController : MonoBehaviour
{
    #region Singleton
    private static LevelController Instance_;
    public static LevelController Instance
    {
        get
        {
            if (Instance_ == null)
            {
                Instance_ = GameObject.FindObjectOfType<LevelController>();
                if (Instance_ == null)
                {
                    throw new ArgumentNullException("No object exists with type LevelController!");
                }
            }
            return Instance_;
        }
    }

    #endregion

    #region Variables
    public static Dictionary<string, string> DynamicVariables = new Dictionary<string, string>();
    private static float Extents;
    private EventDefinition[] EventDefinitions;
    private Level CurrentLevel;
    private float CurrentTime;
    private Queue<LevelFunction> LevelQueue;
    #endregion

    #region Resources
    private AudioClip LevelSong;
    static class Spawn
    {
        #region Assets
        private static Transform Circle;
        private static Transform Square;
        private static Transform BottomPivotSquare;
        #endregion

        #region Variables
        private static Transform ProjectileStorage;
        #endregion

        internal static void LoadResources()
        {
            // Load resources from assets
            Circle = Resources.Load<Transform>("Circle");
            Square = Resources.Load<Transform>("Square");
            BottomPivotSquare = Resources.Load<Transform>("Beam");
            // Get ProjectileStorage
            ProjectileStorage = GameObject.Find("ProjectileStorage").transform;
        }

        private static BezierContour[] CalculateCone(float Angle0, float Angle1, float Size)
        {
            // TODO: MAKE THIS LESS AWFUL BY FIGURING OUT HOW TO USE THE VECTOR LIBRARY BETTER
            float Radius = 180 - Mathf.Abs(Mathf.Abs(Angle0 - Angle1) - 180);
            float DrawAngle = (Angle0 - 90) % 360;
            if (DrawAngle < 0)
            {
                DrawAngle += 360;
            }

            BezierPathSegment[] Segments = new BezierPathSegment[Mathf.RoundToInt(Radius) + 1];
            Segments[0].P0 = Vector2.zero;
            Segments[0].P1 = Vector2.zero;
            Segments[0].P2 = Vector2.zero;

            for (int i = 0; i < Mathf.RoundToInt(Radius); i++)
            {
                Vector2 DirectionVector = new Vector2(Mathf.Cos((DrawAngle + i) * Mathf.Deg2Rad), Mathf.Sin((DrawAngle + i) * Mathf.Deg2Rad)) * Size;
                Segments[i + 1].P0 = DirectionVector;
                Segments[i + 1].P1 = DirectionVector;
                Segments[i + 1].P2 = DirectionVector;
            }

            /*Vector2 DirectionVector1 = new Vector2(Mathf.Cos(DrawAngle * Mathf.Deg2Rad), Mathf.Sin(DrawAngle * Mathf.Deg2Rad)) * Size;
            Segments[1].P0 = DirectionVector1;
            Segments[1].P1 = DirectionVector1;
            Segments[1].P2 = DirectionVector1; //Vector2.zero;

            Vector2 DirectionVector2 = new Vector2(Mathf.Cos((DrawAngle + Radius) * Mathf.Deg2Rad), Mathf.Sin((DrawAngle + Radius) * Mathf.Deg2Rad)) * Size;
            Segments[2].P0 = DirectionVector2;
            Segments[2].P1 = DirectionVector2;
            Segments[2].P2 = DirectionVector2;

            Vector2 DirectionVector3 = new Vector2(Mathf.Cos((DrawAngle + Radius / 2) * Mathf.Deg2Rad), Mathf.Sin((DrawAngle + Radius / 2) * Mathf.Deg2Rad)) * Size;
            Segments[3].P0 = DirectionVector3;
            Segments[3].P1 = Vector2.zero;
            Segments[3].P2 = Vector2.zero;*/
            //Segments[3].P1 = DirectionVector3;
            //Segments[3].P2 = DirectionVector3;

            return new BezierContour[] { new BezierContour() { Segments = Segments } };

            /*BezierPathSegment[] Arc = VectorUtils.MakeArc(Vector2.zero, DrawAngle * Mathf.Deg2Rad, -Radius * Mathf.Deg2Rad, Size);

            return new BezierContour[] { new BezierContour() { Segments = Arc } };*/
        }

        public static Transform CreateOrb(Color OrbColor, Vector2 Size, int Order, bool Damage = true)
        {
            // Spawn the orb into ProjectileStorage
            Transform Orb = Instantiate(Circle, new Vector2(0, 0), Quaternion.identity, ProjectileStorage);
            Orb.localScale = Size;
            Orb.GetComponent<SpriteRenderer>().color = OrbColor;
            Orb.GetComponent<SpriteRenderer>().sortingOrder = Order;
            if (Damage)
            {

            }
            return Orb;
        }

        public static Transform CreateBox(Color BoxColor, Vector2 Size, int Order, bool Damage = true) {
            Transform Box = Instantiate(Square, new Vector2(0, 0), Quaternion.identity, ProjectileStorage);
            Box.localScale = Size;
            Box.GetComponent<SpriteRenderer>().color = BoxColor;
            Box.GetComponent<SpriteRenderer>().sortingOrder = Order;
            if (Damage)
            {
                
            }
            return Box;
        }

        public static Transform CreateBeam(Color BeamColor, Vector2 Size, int Order, bool Damage = true)
        {
            Transform Beam = Instantiate(BottomPivotSquare, new Vector2(0, 0), Quaternion.identity, ProjectileStorage);
            Beam.localScale = Size;
            Beam.GetComponent<SpriteRenderer>().color = BeamColor;
            Beam.GetComponent<SpriteRenderer>().sortingOrder = Order;
            if (Damage)
            {

            }
            return Beam;
        }

        public static Transform CreateCone(Color ConeColor, float StartAngle, float EndAngle, bool Damage = true)
        {
            // TODO: MAKE THIS LESS CRAP
            // Create the object and give it a renderer
            GameObject ConeObject = new GameObject();
            ConeObject.transform.name = "Cone";
            ConeObject.transform.parent = ProjectileStorage;
            SpriteRenderer Renderer = ConeObject.AddComponent<SpriteRenderer>();
            // Calculate how to generate the cone
            Shape Path = new Shape()
            {
                Contours = CalculateCone(StartAngle, EndAngle, 190f),
                Fill = new SolidFill() { Color = ConeColor, Mode = FillMode.OddEven, Opacity = 0.5f },
                PathProps = new PathProperties()
                {
                    Stroke = new Stroke() { Color = Color.black }
                }
            };
            // Tesselate the scene
            Unity.VectorGraphics.Scene VectorScene = new Unity.VectorGraphics.Scene()
            {
                Root = new SceneNode() { Shapes = new List<Shape>() { Path } }
            };
            List<VectorUtils.Geometry> Geometry = VectorUtils.TessellateScene(VectorScene, new VectorUtils.TessellationOptions()
            {
                StepDistance = 100.0f,
                MaxCordDeviation = 0.05f,
                MaxTanAngleDeviation = 0.05f,
                SamplingStepSize = 0.01f
            });
            // Build a sprite
            Sprite ConeSprite = VectorUtils.BuildSprite(Geometry, 10.0f, VectorUtils.Alignment.SVGOrigin, Vector2.zero, 100);
            Renderer.sprite = ConeSprite;
            Renderer.material = new Material(Shader.Find("Unlit/Vector"));
            // Collide
            if (Damage)
            {
                Vector2[] Verticies = new Vector2[]
                {
                    Path.Contours[0].Segments[0].P0,
                    Path.Contours[0].Segments[1].P0 / 9.5f,
                    Path.Contours[0].Segments[Path.Contours[0].Segments.Length / 2].P0 / 9.5f,
                    Path.Contours[0].Segments[Path.Contours[0].Segments.Length - 1].P0 / 9.5f,
                    Path.Contours[0].Segments[0].P0 / 9.5f
                };
                EdgeCollider2D Collider = ConeObject.AddComponent<EdgeCollider2D>();
                Collider.points = Verticies;
            }
            // Return the cone
            return ConeObject.transform;
        }

        public static Transform CreateDottedLine(Color LineColor, float Angle, float Size)
        {
            // Build the dotted line object
            GameObject Object = new GameObject();
            Transform DottedLine = Object.transform;
            // Calculate direction and endpoint
            Vector2 Direction = new Vector2(Mathf.Cos(Angle * Mathf.Deg2Rad), Mathf.Sin(Angle * Mathf.Deg2Rad));
            Vector2 EndPoint = new Vector2(Direction.x * Extents, Direction.y * Extents);
            // Assume spacing
            float Spacing = Size / 1.5f;
            // Generate dots
            Vector2 Point = new Vector2(0, 0);
            while (EndPoint.magnitude > Point.magnitude)
            {
                Transform Dot = Instantiate(Circle, Point, Quaternion.identity, DottedLine);
                Dot.name = "Dot";
                Dot.localScale = new Vector2(Size, Size);
                Dot.GetComponent<SpriteRenderer>().color = LineColor;
                Point += Direction * Spacing;
            }
            return DottedLine;
        }
    };
    static class ProjectileHandler
    {
        public static void Fire(Transform Object, float Speed, float Direction)
        {
            Projectile projectile = Object.gameObject.AddComponent<Projectile>();
            projectile.Speed = Speed;
            projectile.Direction = Direction;
        }
    };
    private IEnumerator LoadSong(string Path)
    {
        AudioClip Clip = null;
        AudioType Type = AudioType.OGGVORBIS;
        if (Path.EndsWith(".mp3") || Path.EndsWith(".mp2"))
        {
            // THIS DOESNT WORK BECAUSE UNITY SUCKS WHAT THE HELL IT CANT LOAD MP3 FILES FROM MY HARD DRIVE!!!
            Type = AudioType.MPEG;
        }
        else if (Path.EndsWith(".wav"))
        {
            Type = AudioType.WAV;
        }
        else if (Path.EndsWith(".ogg"))
        {
            Type = AudioType.OGGVORBIS;
        }
        using (UnityWebRequest Request = UnityWebRequestMultimedia.GetAudioClip(Path, Type))
        {
            Request.SendWebRequest();

            while (!Request.isDone)
            {
                Debug.Log("Download progress " + Request.downloadProgress);

                yield return new WaitForEndOfFrame();
            }

            // Wait for it to finish
            yield return new WaitForEndOfFrame();
            // Now deal with the request

            Debug.Log(Request.isDone);
            Debug.Log(Request.isNetworkError);
            Debug.Log(Request.downloadedBytes);
            Debug.Log(Request.url);
            Debug.Log(Request.responseCode);
            Debug.Log(Request.downloadProgress);

            Clip = DownloadHandlerAudioClip.GetContent(Request);
        }

        LevelSong = Clip;
    }

    public static object StringCast(Type Cast, string Value, Dictionary<string, string> Variables, out bool IsDynamic)
    {
        IsDynamic = false;
        if (Cast == typeof(short) || Cast == typeof(int) || Cast == typeof(long) || Cast == typeof(float) || Cast == typeof(double))
        {
            TypeConverter Converter = TypeDescriptor.GetConverter(Cast);
            if (Converter != null && Converter.IsValid(Value))
            {
                return Converter.ConvertFromString(Value);
            } else
            {
                double Calculation = CalculateMath(Value, Variables);
                if (double.IsNaN(Calculation))
                {
                    IsDynamic = true;
                    return Value;
                } else
                {
                    return Convert.ChangeType(Calculation, Cast);
                }
            }
        } else if (Cast == typeof(Color))
        {
            if (Value.StartsWith("#"))
            {
                Color ColorValue;
                if (!ColorUtility.TryParseHtmlString(Value, out ColorValue))
                {
                    throw new InvalidCastException("Unable to cast color string '" + Value + "' to a color!");
                }
                return ColorValue;
            }
            else if (Value.StartsWith("rgb") || Value.StartsWith("rgba"))
            {
                Color ColorValue;
                string[] ColorValues = Value.Split('(', ')')[1].Split(',');
                ColorValue.r = float.Parse(ColorValues[0]) / 255;
                ColorValue.g = float.Parse(ColorValues[1]) / 255;
                ColorValue.b = float.Parse(ColorValues[2]) / 255;
                if (ColorValues.Length == 4)
                {
                    ColorValue.a = float.Parse(ColorValues[3]) / 255;
                } else
                {
                    ColorValue.a = 1;
                }
                return ColorValue;
            }
            else
            {
                return Cast.GetProperty(Value.ToLower()).GetValue(null, null);
            }
        } else if (Cast == typeof(Vector2))
        {
            string[] Attributes = Value.Split(',');
            float X = (float)StringCast(typeof(float), Attributes[0], Variables, out bool XDynamic);
            float Y = (float)StringCast(typeof(float), Attributes[1], Variables, out bool YDynamic);
            IsDynamic = XDynamic || YDynamic;
            return new Vector2(X, Y);
        } else if (typeof(ILevelParsable).IsAssignableFrom(Cast)) {
            object Instance = Activator.CreateInstance(Cast);
            ((ILevelParsable)Instance).Parse(Value, Variables, ref IsDynamic);
            return Instance;
        } else {
            return Convert.ChangeType(Value, Cast);
        }
    }
    #endregion
    

    // Function that calculates math
    public static double CalculateMath(string Equation, Dictionary<string, string> MathVariables)
    {
        // Create an array for the variables being used
        Argument[] Arguments = new Argument[MathVariables.Count];
        for (int i = 0; i < MathVariables.Count; i++)
        {
            KeyValuePair<string, string> MathVariable = MathVariables.ElementAt(i);
            Arguments[i] = new Argument(MathVariable.Key + " = " + MathVariable.Value);
        }
        // Actually do the expression
        Expression Calculator = new Expression(Equation, Arguments);
        return Calculator.calculate();
    }

    // Recursive function to complie ExecutionSpace
    public void CompileExecutionSpace(ExecutionSpace Space, List<LevelFunction> Functions, Dictionary<string, string> CompilerVariables, Dictionary<int, Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>>> Defaults)
    {
        // Doesn't contain key so that the root space doesn't create another set of defaults
        if (!Defaults.ContainsKey(Space.GetHashCode()))
        {
            Defaults.Add(Space.GetHashCode(), new Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>>(Defaults[Space.Parent.GetHashCode()]));
        }
        for (int i = 0; i < Space.Nodes.Count; i++)
        {
            // Make an array of values flagged to be dynamic
            Dictionary<string, Type>  DynamicFlags = new Dictionary<string, Type>();
            // Get the node and give it a dictionary of all the values defined
            GameEvent Node = Space.Nodes[i];
            Dictionary<string, object> Defined = new Dictionary<string, object>();
            // Iterate through all the definition properties and either set the node properties to default or to a defined property
            foreach (KeyValuePair<string, KeyValuePair<Type, object>> Property in Node.Definition.Properties)
            {
                if (Node.Attributes.ContainsKey(Property.Key))
                {
                    object Value = StringCast(Property.Value.Key, Node.Attributes[Property.Key], CompilerVariables, out bool IsDynamic);
                    if (IsDynamic)
                    {
                        if (Node.Definition.AcceptsDynamicValues)
                        {
                            DynamicFlags[Property.Key] = Property.Value.Key;
                        } else
                        {
                            // Just set it to default so further errors don't occur
                            Defined[Property.Key] = Defaults[Space.GetHashCode()][Node.Definition.NodeName][Property.Key].Value;
                            throw new InvalidOperationException("Node of type '" + Node.Definition.NodeName + "' does not accept dynamic variables! setting value to default.");
                        }
                    }
                    Defined[Property.Key] = Value;
                } else
                {
                    Defined[Property.Key] = Defaults[Space.GetHashCode()][Node.Definition.NodeName][Property.Key].Value;
                }
            }
            // Now we add more stuff to Defined if the definition accepts extra data
            if (Node.Definition.AcceptsExtraData)
            {
                foreach (KeyValuePair<string, string> Attribute in Node.Attributes) {
                    if (!Defined.ContainsKey(Attribute.Key))
                    {
                        Defined[Attribute.Key] = Attribute.Value;
                    }
                }
            }

            // Now determine if it's some kind of special node and put it in like that
            if (Node.Definition.NodeName == "wait")
            {
                // If it was a dynamic value then deal with it differently by putting
                // it as a standard type value and stuff
                Space.Time.CurrentTime += (float)Defined["time"];
            } else if (Node.Definition.NodeName == "default")
            {
                //Screen.currentResolution.refreshRate;
                string Type = (string)Defined["type"];

                List<Dictionary<string, KeyValuePair<Type, object>>> UsedDefaults = new List<Dictionary<string, KeyValuePair<Type, object>>>();
                foreach (EventDefinition Definition in EventDefinitions)
                {
                    if (Definition.UseDefaults == Type)
                    {
                        UsedDefaults.Add(Defaults[Space.GetHashCode()][Definition.NodeName]);
                    }
                }

                foreach (KeyValuePair<string, object> Default in Defined)
                {
                    if (Default.Key != "type")
                    {
                        object Value = StringCast(UsedDefaults[0][Default.Key].Key, (string)Default.Value, CompilerVariables, out bool IsDynamic);
                        if (IsDynamic)
                        {
                            throw new InvalidOperationException("Defaults does not accept dynamic variables! at defaults for '" + Default.Key + "'");
                        } else
                        {
                            for (int j = 0; j < UsedDefaults.Count; j++)
                            {
                                if (UsedDefaults[j][Default.Key].Key == UsedDefaults[0][Default.Key].Key)
                                {
                                    UsedDefaults[j][Default.Key] = new KeyValuePair<Type, object>(UsedDefaults[0][Default.Key].Key, Value);
                                }
                                else
                                {
                                    throw new TypeAccessException("Value using specific type of defaults has a different type then required (This is probably the developer's fault, not yours. If you see this please report it)");
                                }
                            }
                        }
                    }
                }
                // Set defaults or something
            } else
            {
                LevelFunction Function = new LevelFunction(Node.Definition.NodeName, Space.Time.CurrentTime, Defined, DynamicFlags, new Dictionary<string, string>(CompilerVariables));
                Functions.Add(Function);
            }
            // Deal with the node body
            if (Node.Body != null)
            {
                if (Node.Definition.NodeName == "do")
                {
                    Node.Body.Time.CurrentTime = Space.Time.CurrentTime;
                    CompileExecutionSpace(Node.Body, Functions, CompilerVariables, Defaults);
                } else if (Node.Definition.NodeName == "at")
                {
                    Node.Body.Time.CurrentTime = Space.Time.CurrentTime + (float)Defined["time"];
                    CompileExecutionSpace(Node.Body, Functions, CompilerVariables, Defaults);
                } else if (Node.Definition.NodeName == "repeat")
                {
                    // Define the variable for the iterator
                    CompilerVariables[(string)Defined["iterator"]] = "0";
                    for (int j = 0; j < (int)Defined["times"]; j++)
                    {
                        // Increase the iterator and continually re-run execution space compliation
                        CompilerVariables[(string)Defined["iterator"]] = j.ToString();
                        CompileExecutionSpace(Node.Body, Functions, CompilerVariables, Defaults);
                        // It's in a loop and the defaults might change each iteration so prevent
                        // the loop from keeping old defaults due to it not reassigning them and
                        // also to make sure this system is proof to any new operators and functions
                        // that could possibly be added in the future.
                        Defaults.Remove(Node.Body.GetHashCode());
                    }
                    // Remove the iteration variable because it is no longer in the space
                    CompilerVariables.Remove((string)Defined["iterator"]);
                }
                // Do some more stuff here maybe
            }
        }

    }

    // Function which activates the entire game based on XML
    public Level ParseLevelXML(string path) 
    {
        // Create the thing to read the level XML
        XmlReaderSettings Settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };
        XmlReader Reader = XmlReader.Create(path, Settings);
        // Create main variables used for handling data
        ExecutionSpace Space = new ExecutionSpace(null, new TimeBlock());
        Exception Error = null;
        // Make required variables
        string LevelName = "";
        string LevelCreator = "";
        string LevelSong = "";
        // Read the level XML
        while (Reader.Read()) {
            string Name = Reader.Name.ToLower();
            // Do not do anything with level
            if (Name == "level")
            {
                // If it's EndElement it messes up all the values
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    LevelName = Reader.GetAttribute("name");
                    LevelCreator = Reader.GetAttribute("creator");
                    LevelSong = Reader.GetAttribute("song");
                }
            }
            else if (Name != "xml")
            {
                EventDefinition Definition = EventDefinitions.FirstOrDefault(t => t.NodeName == Name);
                if (Definition != default)
                {
                    // Get all of the attributes from the XML
                    Dictionary<string, string> Attributes = new Dictionary<string, string>();
                    for (int i = 0; i < Reader.AttributeCount; i++)
                    {
                        Reader.MoveToAttribute(i);
                        if (Definition.Properties.ContainsKey(Reader.Name) || Definition.AcceptsExtraData)
                        {
                            Attributes[Reader.Name] = Reader.Value;
                        } else
                        {
                            Error = new ArgumentNullException("No property under the name of attribute '" + Reader.Name + "' exists on the node of type '" + Definition.NodeName + "'!");
                            break;
                        }
                    }
                    Reader.MoveToElement();
                    // Feed the attributes to a new GameEvent
                    GameEvent Event = Space.CreateEvent(Definition, Attributes);
                    // Handle if the element has a body
                    if (Definition.HasBody)
                    {
                        if (Reader.NodeType == XmlNodeType.Element)
                        {
                            ExecutionSpace DefinitionSpace = new ExecutionSpace(Event, Definition.NewTimeBlock ? new TimeBlock(Space.Time.CurrentTime) : Space.Time);
                            DefinitionSpace.Parent = Space;
                            Space = DefinitionSpace;
                        }
                        else if (Reader.NodeType == XmlNodeType.EndElement)
                        {
                            Space = Space.Parent;
                        }
                    }
                }
                else
                {
                    Error = new KeyNotFoundException("Definition for level event '" + Name + "' doesn't exist!");
                }
            }
            // Break if any error appears
            if (Error != null)
            {
                break;
            }
        }
        if (Error != null)
        {
            Debug.LogError(Error);
        }
        // Now actually handle all of the ExecutionSpace stuff
        List<LevelFunction> Functions = new List<LevelFunction>();
        Dictionary<string, string> ComplierVariables = new Dictionary<string, string>();
        // Implement defaults
        Dictionary<int, Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>>> Defaults = new Dictionary<int, Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>>>();
        Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>> RootDefaults = new Dictionary<string, Dictionary<string, KeyValuePair<Type, object>>>();
        for (int i = 0; i < EventDefinitions.Length; i++)
        {
            EventDefinition Definition = EventDefinitions[i];
            Dictionary<string, KeyValuePair<Type, object>> DefinitionDefaults = new Dictionary<string, KeyValuePair<Type, object>>();
            foreach (KeyValuePair<string, KeyValuePair<Type, object>> PropertyType in Definition.Properties)
            {
                DefinitionDefaults[PropertyType.Key] = new KeyValuePair<Type, object>(PropertyType.Value.Key, PropertyType.Value.Value);
            }
            RootDefaults[Definition.NodeName] = DefinitionDefaults;
        }
        Defaults[Space.GetHashCode()] = RootDefaults;
        // Compile everything
        CompileExecutionSpace(Space, Functions, ComplierVariables, Defaults);
        // Sort the game functions
        Functions.Sort();
        // Now convert functions to a queue
        Queue<LevelFunction> Map = new Queue<LevelFunction>(Functions);
        // Create the level map
        Level GameLevel = new Level(LevelName, LevelCreator, LevelSong, Map);
        return GameLevel;
    }

    // Execute a LevelFunction directly
    public IEnumerator ExecuteLevelFunction(LevelFunction Function)
    {
        Dictionary<string, object> Properties = new Dictionary<string, object>(Function.Properties);
        // Deal with dynamic values
        if (Function.DynamicFlags.Count > 0)
        {
            Dictionary<string, string> UseVariables = new Dictionary<string, string>(DynamicVariables);
            foreach (KeyValuePair<string, string> CompilerVariable in Function.ComplierVariables)
            {
                UseVariables[CompilerVariable.Key] = CompilerVariable.Value;
            }

            foreach (KeyValuePair<string, Type> DynamicFlag in Function.DynamicFlags)
            {
                Properties[DynamicFlag.Key] = StringCast(DynamicFlag.Value, (string)Properties[DynamicFlag.Key], UseVariables, out bool OperationFailed);
                if (OperationFailed)
                {
                    throw new InvalidOperationException("Unable to execute dynamic variable operation '" + (string)Properties[DynamicFlag.Key] + "', most likely due to variable being used in operation is non-existent or misspelled");
                }
            }
        }
        /*Debug.Log(Properties);
        foreach (KeyValuePair<string, object> value in Properties)
        {
            Debug.Log(value.Key + " = " + value.Value);
        }*/
        // Now switch based off of what kind of value it is
        switch(Function.Name)
        {
            case "debug":
                {
                    Debug.Log((string)Properties["message"]);
                }
                break;
            case "color":
                {
                    string Object = (string)Properties["object"];                    
                    if (Object == "Background")
                    {
                        Color color = (Color)Properties["Color"];
                        Camera.main.backgroundColor = color;
                    } 
                    else if (Object == "Core")
                    {
                        Color color = (Color)Properties["color"];
                        GetComponent<SpriteRenderer>().color = color;
                    }
                    else if (Object == "Player")
                    {
                        Color color = (Color)Properties["color"];
                        GameObject.Find("Player").GetComponent<Player>().ChangeColor(color);
                    }
                    else
                    {
                        if (Object.EndsWith("Ring"))
                        {
                            Color color = (Color)Properties["color"];
                            GameObject RingObject = GameObject.Find("/Rings/" + Object);
                            RingObject.GetComponent<SpriteRenderer>().color = color;
                        }
                        else if (Object.EndsWith("Circle"))
                        {
                            Color color1 = (Color)Properties["color1"];
                            Color color2 = (Color)Properties["color2"];
                            GameObject CircleObject = GameObject.Find("/Circles/" + Object);
                            CircleObject.transform.Find("Color1").GetComponent<SpriteRenderer>().color = color1;
                            CircleObject.transform.Find("Color2").GetComponent<SpriteRenderer>().color = color2;
                        }
                    }
                }
                break;
            case "units":
                {
                    int Amount = (int)Properties["amount"];
                    GameObject.Find("Player").GetComponent<Player>().SetUnitCount(Amount);
                }
                break;
            case "speed":
                {
                    float Amount = (float)Properties["amount"];
                    // Speed value of 1 is normal
                    GameObject.Find("Player").GetComponent<Player>().Speed = Amount;
                }
                break;
            case "spin":
                {
                    float Speed = (float)Properties["speed"];
                    foreach (Transform Circle in GameObject.Find("Circles").transform)
                    {
                        Circle.GetComponent<Spin>().Speed = Speed;
                    }
                }
                break;
            case "flash":
                {
                    // TODO: Allow multiple flash at once perhaps.
                    bool Overtop = (bool)Properties["overtop"];
                    float Duration = (float)Properties["duration"];
                    Color ScreenColor = (Color)Properties["color"];
                    Easing EaseOut = (Easing)Properties["easeOut"];
                    Easing EaseIn = (Easing)Properties["easeIn"];
                    // Flash the screen
                    Color OldBackgroundColor;
                    if (Overtop)
                    {
                        // TODO: Add overtop system (so that instead of just changing behind the thing it changes at different levels or something (might make it an int and not boolean))
                        OldBackgroundColor = Color.black;
                    } else
                    {
                        OldBackgroundColor = Camera.main.backgroundColor;
                    }
                    EaseIn.Run(progress =>
                    {
                        Camera.main.backgroundColor = Color.Lerp(OldBackgroundColor, ScreenColor, progress);
                    });
                    yield return new WaitForSeconds(Duration);
                    EaseOut.Run(progress =>
                    {
                        Camera.main.backgroundColor = Color.Lerp(ScreenColor, OldBackgroundColor, progress);
                    });
                } break;
            // Attack types
            case "orb":
                {
                    Color OrbColor = (Color)Properties["color"];
                    Vector2 OrbSize = (Vector2)Properties["size"];
                    int OrbOrder = (int)Properties["order"];
                    float Speed = (float)Properties["speed"];
                    float Direction = (float)Properties["direction"];
                    // Spawn orbs
                    Transform Orb = Spawn.CreateOrb(OrbColor, OrbSize, OrbOrder);
                    ProjectileHandler.Fire(Orb, Speed, Direction);
                }
                break;
            case "orb_spread":
                {
                    Color OrbColor = (Color)Properties["color"];
                    Vector2 OrbSize = (Vector2)Properties["size"];
                    int OrbOrder = (int)Properties["order"];
                    float Speed = (float)Properties["speed"];
                    float Direction = (float)Properties["direction"];
                    float Spacing = (float)Properties["spacing"];
                    int Count = (int)Properties["count"];
                    // Spawn orbs
                    float AngleEnding = 0f;
                    if (Count % 2 == 0)
                    {
                        AngleEnding = (Count * Spacing / 2) - (Spacing / 2);
                    } else if (Count % 2 != 0)
                    {
                        AngleEnding = (Count - 1) * Spacing / 2;
                    }
                    for (float i = -AngleEnding; i <= AngleEnding; i += Spacing)
                    {
                        float Angle = i + Direction;
                        Transform Orb = Spawn.CreateOrb(OrbColor, OrbSize, OrbOrder);
                        ProjectileHandler.Fire(Orb, Speed, Angle);
                    }
                }
                break;
            case "laser":
                {
                    Color LaserColor = (Color)Properties["color"];
                    float Angle = (float)Properties["angle"];
                    float Width = (float)Properties["width"];
                    float Lifetime = (float)Properties["lifetime"];
                    int Order = (int)Properties["order"];
                    Easing EaseOut = (Easing)Properties["easeOut"];
                    // Spawn box for cutter
                    Transform Beam = Spawn.CreateBeam(LaserColor, new Vector2(0.5f, 0.5f), Order);
                    Beam.RotateAround(transform.position, new Vector3(0, 0, -1f), Angle);
                    Beam.localScale = new Vector2(Width, Extents);
                    yield return new WaitForSeconds(Lifetime);
                    EaseOut.Run(progress =>
                    {
                        if (progress >= 1)
                        {
                            Destroy(Beam.gameObject);
                        } else
                        {
                            Beam.localScale = Vector2.Lerp(new Vector2(Width, Extents), new Vector2(0, Extents), progress);
                        }
                    });
                    
                } break;
            // Indicators
            case "indicator":
                {
                    Color IndicatorColor = (Color)Properties["color"];
                    float StartAngle = (float)Properties["startAngle"];
                    float EndAngle = (float)Properties["endAngle"];
                    float Lifetime = (float)Properties["lifetime"];
                    // Spawn cone
                    Transform Cone = Spawn.CreateCone(IndicatorColor, StartAngle, EndAngle);
                    // Apply death clock to cone
                    DeathClock Death = Cone.gameObject.AddComponent<DeathClock>();
                    Death.Lifetime = Lifetime;
                }
                break;
            case "dotted_line":
                {
                    Color IndicatorColor = (Color)Properties["color"];
                    float Angle = (float)Properties["angle"];
                    float Size = (float)Properties["size"];
                    float Lifetime = (float)Properties["lifetime"];
                    // Spawn
                    Transform DottedLine = Spawn.CreateDottedLine(IndicatorColor, Angle, Size);
                    // Apply death clock
                    DeathClock Death = DottedLine.gameObject.AddComponent<DeathClock>();
                    Death.Lifetime = Lifetime;
                } break;
        }
        yield return null;

    }

    // Set a level to begin playing
    public IEnumerator ExecuteLevel(Level GameLevel)
    {
        yield return StartCoroutine(LoadSong(GameLevel.Song));
        CurrentLevel = GameLevel;
        CurrentTime = 0;
        LevelQueue = new Queue<LevelFunction>(CurrentLevel.Map);
        GetComponent<AudioSource>().clip = LevelSong;
        GetComponent<AudioSource>().Play();
    }

    // Function to handle deloading the game queue and making sure all functions that need to run, run
    void UpdateLevelFunctions()
    {
        if (LevelQueue.Peek().Time <= CurrentTime)
        {
            StartCoroutine(ExecuteLevelFunction(LevelQueue.Dequeue()));
            if (LevelQueue.Any())
            {
                UpdateLevelFunctions();
            }
        }
    }

    // Update the game
    void Update()
    {
        // Only do stuff when the level is running
        if (CurrentLevel != null)
        {
            if (!LevelQueue.Any())
            {
                // Level is done, end it
                CurrentLevel = null;
            }
            else
            {
                // Update runtime variables for dynamic variable based level functions
                DynamicVariables["player_direction"] = Player.Angle.ToString();
                // Automatically increment the game timer
                CurrentTime += Time.deltaTime;
                // Now run functions
                UpdateLevelFunctions();
            }
        }
    }

    /*IEnumerator ExecuteGameAction(GameAction action) {
        yield return new WaitForSeconds(action.Time);
        switch(action.Name)
        {
            case "color":
                {
                    string Object = action.GetProperty<string>("object", "core");
                    if (Object == "Core")
                    {
                        GetComponent<SpriteRenderer>().color = action.GetProperty<Color>("color", Color.black);
                    }
                    else if (Object == "Player")
                    {
                        GameObject.Find("Player").GetComponent<Player>().ChangeColor(action.GetProperty<Color>("color", Color.black));
                    }
                    else
                    {
                        if (Object.EndsWith("Ring"))
                        {
                            GameObject RingObject = GameObject.Find("/Rings/" + Object);
                            RingObject.GetComponent<SpriteRenderer>().color = action.GetProperty<Color>("color", Color.black);
                        }
                        else if (Object.EndsWith("Circle"))
                        {
                            GameObject CircleObject = GameObject.Find("/Circles/" + Object);
                            CircleObject.transform.Find("Color1").GetComponent<SpriteRenderer>().color = action.GetProperty<Color>("color1", Color.black);
                            CircleObject.transform.Find("Color2").GetComponent<SpriteRenderer>().color = action.GetProperty<Color>("color2", Color.black);
                        }
                    }
                }
                break;
            case "units":
                {
                    int Amount = action.GetProperty<int>("amount", 1);
                    GameObject.Find("Player").GetComponent<Player>().SetUnitCount(Amount);
                }
                break;
            case "spin":
                {
                    float Speed = action.GetProperty<float>("speed", 1);
                    foreach (Transform circle in GameObject.Find("Circles").transform)
                    {
                        circle.GetComponent<Spin>().Speed = Speed;
                    }
                }
                break;
            case "orb":
                {
                    Color OrbColor = action.GetProperty<Color>("color", Color.black);
                    Vector2 OrbSize = action.GetProperty<Vector2>("size", new Vector2(1, 1));
                    int OrbOrder = action.GetProperty<int>("order", 2);
                    float Speed = action.GetProperty<float>("speed", 5);
                    float Direction = action.GetProperty<float>("direction", 0);
                    Transform Orb = Spawn.CreateOrb(OrbColor, OrbSize, OrbOrder);
                    ProjectileHandler.Fire(Orb, Speed, Direction);
                }
                break;
            case "orb_spread":
                {
                    Color OrbColor = action.GetProperty<Color>("color", Color.black);
                    Vector2 OrbSize = action.GetProperty<Vector2>("size", new Vector2(1, 1));
                    int OrbOrder = action.GetProperty<int>("order", 2);
                    float Direction = action.GetProperty<float>("direction", 0);
                    float Speed = action.GetProperty<float>("speed", 5);
                    int Count = action.GetProperty<int>("count", 0);
                    float Interval = action.GetProperty<float>("interval", 0);
                    float Delay = action.GetProperty<float>("delay", 0);
                    if (Count % 2 != 0)
                    {
                        // Odd number
                        int OrbsOnEachSize = (Count / 2);
                        for (int i = -OrbsOnEachSize; i <= OrbsOnEachSize; i++)
                        {
                            float Angle = Direction + (i * Interval);
                            Transform Orb = Spawn.CreateOrb(OrbColor, OrbSize, OrbOrder);
                            ProjectileHandler.Fire(Orb, Speed, Angle);
                            if (Delay != 0)
                            {
                                yield return new WaitForSeconds(Delay);
                            }
                        }
                    } else if (Count % 2 == 0)
                    {
                        // Even number
                        int OrbsOnEachSize = (Count / 2);
                        for (int i = -OrbsOnEachSize; i <= OrbsOnEachSize - 1; i++)
                        {
                            float Angle = Direction + (Interval / 2) + (i * Interval - Interval / 2);
                            Transform Orb = Spawn.CreateOrb(OrbColor, OrbSize, OrbOrder);
                            ProjectileHandler.Fire(Orb, Speed, Angle);
                            if (Delay != 0)
                            {
                                yield return new WaitForSeconds(Delay);
                            }
                        }
                    }
                }
                break;
            default:
                throw new InvalidOperationException("Unable to execute GameAction of uknown type '" + action.Name + "'");
        }
    }*/

    // Load in game essentials
    void Awake()
    {
        // Load resources for internal helper classes
        Spawn.LoadResources();
        // Figure out screen space extents
        Extents = Mathf.Max(Camera.main.orthographicSize, Camera.main.orthographicSize * Screen.width / Screen.height);
        // Create standard dynamic variables
        DynamicVariables.Add("player_direction", "0");
        // Create the EventDefinitions array
        EventDefinitions = new EventDefinition[]
        {
            // Required expressions for game (special types of blocks)
            new EventDefinition("at", true, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"time", new KeyValuePair<Type, object>(typeof(float), 0f) }
            })
            {
                NewTimeBlock = true,
                AcceptsDynamicValues = false
            },
            new EventDefinition("do", true, new Dictionary<string, KeyValuePair<Type, object>>()) {
                NewTimeBlock = true
            },
            new EventDefinition("repeat", true, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"times", new KeyValuePair<Type, object>(typeof(int), 0) },
                {"iterator", new KeyValuePair<Type, object>(typeof(string), "i") }
            })
            {
                AcceptsDynamicValues = false
            },
            new EventDefinition("wait", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"time", new KeyValuePair<Type, object>(typeof(float), 0f) }
            })
            {
                AcceptsDynamicValues = false
            },
            new EventDefinition("default", false, new Dictionary<string, KeyValuePair<Type, object>> {
                {"type", new KeyValuePair<Type, object>(typeof(string), "") }
            })
            {
                AcceptsDynamicValues = false,
                AcceptsExtraData = true
            },
            // General functions
            new EventDefinition("debug", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"message", new KeyValuePair<Type, object>(typeof(string), "") }
            }),
            new EventDefinition("color", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"object", new KeyValuePair<Type, object>(typeof(string), "") },
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"color1", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"color2", new KeyValuePair<Type, object>(typeof(Color), Color.black) }
            }),
            new EventDefinition("units", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"amount", new KeyValuePair<Type, object>(typeof(int), 0) }
            }),
            new EventDefinition("speed", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"amount", new KeyValuePair<Type, object>(typeof(float), 0)  }
            }),
            new EventDefinition("spin", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"speed", new KeyValuePair<Type, object>(typeof(float), 0f) }
            }),
            new EventDefinition("flash", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"overtop", new KeyValuePair<Type, object>(typeof(bool), 0f) },
                {"duration", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"easeIn", new KeyValuePair<Type, object>(typeof(Easing), new Easing("linear", 0)) },
                {"easeOut", new KeyValuePair<Type, object>(typeof(Easing), new Easing("linear", 0)) }
            }),
            // Attack types
            new EventDefinition("orb", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"size", new KeyValuePair<Type, object>(typeof(Vector2), Vector2.one) },
                {"order", new KeyValuePair<Type, object>(typeof(int), 0) },
                {"speed", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"direction", new KeyValuePair<Type, object>(typeof(float), 0f) }
            }),
            new EventDefinition("orb_spread", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"size", new KeyValuePair<Type, object>(typeof(Vector2), Vector2.one) },
                {"order", new KeyValuePair<Type, object>(typeof(int), 0) },
                {"speed", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"direction", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"spacing", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"count", new KeyValuePair<Type, object>(typeof(int), 0f) }
            })
            {
                UseDefaults = "orb"
            },
            new EventDefinition("laser", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"angle", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"width", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"lifetime", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"order", new KeyValuePair<Type, object>(typeof(int), 0) },
                {"easeOut", new KeyValuePair<Type, object>(typeof(Easing), new Easing("linear", 0)) }
            }),
            // Indicators
            new EventDefinition("indicator", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"startAngle", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"endAngle", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"lifetime", new KeyValuePair<Type, object>(typeof(float), 0f) }
            }),
            new EventDefinition("dotted_line", false, new Dictionary<string, KeyValuePair<Type, object>>
            {
                {"color", new KeyValuePair<Type, object>(typeof(Color), Color.black) },
                {"angle", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"size", new KeyValuePair<Type, object>(typeof(float), 0f) },
                {"lifetime", new KeyValuePair<Type, object>(typeof(float), 0f) }
            })
        };
    }

    // Start up the game (right now we just do testing)
    void Start()
    {
        Level GameLevel = ParseLevelXML(@"C:\Users\Public\Levels\example.xml");
        StartCoroutine(ExecuteLevel(GameLevel));
    }
}
