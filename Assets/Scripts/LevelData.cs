using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using org.mariuszgromada.math.mxparser;

namespace LevelData
{
    public class TimeBlock
    {
        public float CurrentTime;

        public TimeBlock(float CurrentTime_ = 0)
        {
            CurrentTime = CurrentTime_;
        }
    };
    
    public class ExecutionSpace
    {
        public List<GameEvent> Nodes = new List<GameEvent>();
        public GameEvent Node;
        public TimeBlock Time;
        public ExecutionSpace Parent;

        public ExecutionSpace(GameEvent Node_, TimeBlock Time_)
        {
            Node = Node_;
            Time = Time_;
            if (Node != null)
            {
                Node.Body = this;
            }
        }

        public GameEvent CreateEvent(EventDefinition Definition_, Dictionary<string, string> Attributes_)
        {
            GameEvent Event = new GameEvent(Definition_, Attributes_);
            Nodes.Add(Event);
            return Event;
        }

        public override bool Equals(object obj)
        {
            return obj is ExecutionSpace space &&
                   EqualityComparer<List<GameEvent>>.Default.Equals(Nodes, space.Nodes) &&
                   EqualityComparer<GameEvent>.Default.Equals(Node, space.Node) &&
                   EqualityComparer<TimeBlock>.Default.Equals(Time, space.Time) &&
                   EqualityComparer<ExecutionSpace>.Default.Equals(Parent, space.Parent);
        }

        public override int GetHashCode()
        {
            int hashCode = 989138664;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<GameEvent>>.Default.GetHashCode(Nodes);
            hashCode = hashCode * -1521134295 + EqualityComparer<GameEvent>.Default.GetHashCode(Node);
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeBlock>.Default.GetHashCode(Time);
            hashCode = hashCode * -1521134295 + EqualityComparer<ExecutionSpace>.Default.GetHashCode(Parent);
            return hashCode;
        }
    };

    public struct EventDefinition
    {
        public string NodeName;
        public bool HasBody;
        public Dictionary<string, KeyValuePair<Type, object>> Properties;
        public bool NewTimeBlock;

        public bool AcceptsExtraData;
        public bool AcceptsDynamicValues;
        public bool UseStartPosition;

        public string UseDefaults;

        public EventDefinition(string NodeName_, bool HasBody_, Dictionary<string, KeyValuePair<Type, object>> Properties_)
        {
            NodeName = NodeName_;
            HasBody = HasBody_;
            Properties = Properties_;

            AcceptsExtraData = false;
            AcceptsDynamicValues = true;
            NewTimeBlock = false;
            UseStartPosition = false;

            UseDefaults = NodeName;
        }

        public override bool Equals(object obj)
        {
            return obj is EventDefinition definition &&
                   NodeName == definition.NodeName &&
                   HasBody == definition.HasBody &&
                   EqualityComparer<Dictionary<string, KeyValuePair<Type, object>>>.Default.Equals(Properties, definition.Properties) &&
                   NewTimeBlock == definition.NewTimeBlock;
        }

        public override int GetHashCode()
        {
            int hashCode = 1931770830;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NodeName);
            hashCode = hashCode * -1521134295 + HasBody.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, KeyValuePair<Type, object>>>.Default.GetHashCode(Properties);
            hashCode = hashCode * -1521134295 + NewTimeBlock.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(EventDefinition a, EventDefinition b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(EventDefinition a, EventDefinition b)
        {
            return !a.Equals(b);
        }
    };

    public class GameEvent
    {
        public EventDefinition Definition;
        public Dictionary<string, string> Attributes;
        public ExecutionSpace Body;

        public GameEvent(EventDefinition Definition_, Dictionary<string, string> Attributes_)
        {
            Definition = Definition_;
            Attributes = Attributes_;
            Body = null;
        }
    };
};