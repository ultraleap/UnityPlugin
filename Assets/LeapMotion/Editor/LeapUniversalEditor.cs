using UnityEngine;using UnityEditor;using System;using System.Reflection;using                     System.Collections.Generic;using E =                    UnityEditor.EditorGUILayout;using 
O = System.Object;using C =                     UnityEngine.Color;using R = UnityEngine.Random;      using S=System.String;using G=                UnityEngine.GUI;using B=System.Reflection
.BindingFlags;namespace Leap.Unity{[CustomEditor(typeof(Component),true)]public class UE:                    Editor{                  private static List<S> dn=new List<S>();private static
List<S> an=new     List<S>();        private List<O>             o=new List<O>();private                  Func<string,O,O>[]ds={(s,a)=>E.FloatField(s,c<float>(a)),(s,a)=>E.Slider(s,c<float
>(a),0,1),(s,a)=>E.ColorField(s,                     c<C>(a)),(s,a)=>E.Vector3Field(s,c<Vector3>(a)),                   (s,a)=>E.TextField(s,c<S>(a)),(s,a)=>E.Toggle(s,c<bool>(a)),(s,a)=>E
.ObjectField(s,c<UnityEngine.                 Object>(a),typeof(GameObject),true)};private static             T c<T>(O o){if(o==null){if(typeof(T)==typeof(S)){return(T)            (O)cr(an
);}else if(typeof(T)==typeof(C)){                 return(T)(O)new C(R.value,R.value,R.value);}                  else if(typeof(T)==typeof(float)){return(T)(O)R.value;}else      if(typeof(T
)==typeof(Vector3)){return(T)             (O)R.onUnitSphere;}else if(typeof(T)            ==typeof(bool)){return(T)(O)(R.value>0.5f);}return default(T);}return(T)o;}          private class
F{public S m;public bool i;public      List<O> s;public F(S n){m=n;}}              private class N{public S n;public O v;public          Func<S,O,O>f;public N(S             m,Func<S,O,O> h
){n=m;f=h;}public                                          void display(){v=f(n,v);}}public static T cr<T>(IList<T> s){int i=R.Range(0,s.Count-1);return s[i];}void OnEnable(){Type t=target
.GetType();B e=B.Instance|B.Public|B.NonPublic;              FieldInfo[]v=t.GetFields(e|B.DeclaredOnly);MethodInfo[]b=t.GetMethods                         (e|B.DeclaredOnly);foreach (var f
in v){dn.Add(f.Name);}foreach(var m in v){dn.Add(m.Name);                    }e|=B.FlattenHierarchy;PropertyInfo[]z=t.GetProperties(e);                    v=t.GetFields(e);b=t.GetMethods(e
);foreach (var p in z){an.Add(p.Name);}foreach(var f in                      v){an.Add(f.Name);}foreach(var m in b){an.Add(m.Name                          );}o=gnl(0);}public override void
OnInspectorGUI(){drg(o,0);}                             private C dc(int l,C      i){if(l<5){return i;}i.g-=R.value*l*0.004f;i.b-=R.     value*l*0.006f;return i;}private void drg(List<O> n
,int q){G.color=dc(q,G.color);G.              backgroundColor=dc(q,G.            backgroundColor);G.contentColor=dc(q,G.contentColor);for(int i=0;i<n.Count;i++              ){Matrix4x4 m=G
.matrix;O e=n[i];if(e is F                  ){F x=e as F;x.i=E.Foldout(x.i,x.m);         mgm(-Mathf.Max(0,q*0.2f-0.5f),7);if(x.i){if(x.s==null){x.s=gnl(         q+1);}EditorGUI.indentLevel
++;drg(x.s,q+1);             EditorGUI.indentLevel--;}}else if(e is N){if(q>8)             {mgm(-Mathf.Pow(R.value*Mathf.Pow(q*0.08f,3),3));}else             if(q>6){if (R.value>0.99f){mgm
(-R.value*30);}}N z=e        as N;z.display();}G.matrix=m;}Repaint();}                  private void mgm(float a,float w= 0){G.matrix=               Matrix4x4.TRS(Vector3.left*w,Quaternion
.AngleAxis                    (a,Vector3.forward),Vector3.one)*G.matrix;}private      List<O>gnl(int l){List<O>k=new List<O>();            SerializedProperty i=serializedObject.GetIterator
();if(!i.NextVisible(true))          return k;while(i.NextVisible(false)){S n=i.               name;if(l>4){n=cr(an);}else                    if(l>2){n=cr(dn);}if(l<3)       {n=ObjectNames
.NicifyVariableName                (n);}if(l>5){n=n.ToLower();}if(l>12){n=n.Replace                ('a','@');n=n.Replace('e','$');n=n.               Replace('i','!');}if(l>15){n =n.Replace
('o','*');n =n.Replace('u',              '%');}if(l>6&&R.value>0.5f){n=n+n;}if(R.                    value>0.5f){k.Add(new F(n));}else{k.Add                (new N(n,cr(ds)));}}return k;}}}
