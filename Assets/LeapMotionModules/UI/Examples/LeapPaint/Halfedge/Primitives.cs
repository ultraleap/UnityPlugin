using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  public static class Primitives {

    private static List<Face> s_faceCache = new List<Face>();

    public static void AddTetrahedron(HalfedgeMesh mesh) {
      Vertex a = new Vertex(-1F, 0F, -1 / Mathf.Sqrt(2));
      Vertex b = new Vertex(1F, 0F, -1 / Mathf.Sqrt(2));
      Vertex c = new Vertex(0F, 1F, 1 / Mathf.Sqrt(2));
      Vertex d = new Vertex(0F, -1F, 1 / Mathf.Sqrt(2));

      Face ABC = new Face();
      Halfedge ab = new Halfedge();
      Halfedge bc = new Halfedge();
      Halfedge ca = new Halfedge();
      Vertex ABCa = a, ABCb = b, ABCc = c;

      Face ACD = new Face();
      Halfedge ac = new Halfedge();
      Halfedge cd = new Halfedge();
      Halfedge da = new Halfedge();
      Vertex ACDa = Vertex.Copy(a), ACDc = Vertex.Copy(c), ACDd = Vertex.Copy(d);

      Face ADB = new Face();
      Halfedge ad = new Halfedge();
      Halfedge db = new Halfedge();
      Halfedge ba = new Halfedge();
      Vertex ADBa = Vertex.Copy(a), ADBd = Vertex.Copy(d), ADBb = Vertex.Copy(b);

      Face BDC = new Face();
      Halfedge bd = new Halfedge();
      Halfedge dc = new Halfedge();
      Halfedge cb = new Halfedge();
      Vertex BDCb = Vertex.Copy(b), BDCd = Vertex.Copy(d), BDCc = Vertex.Copy(c);

      ABC.halfedge = ab;
      ABCa.halfedge = ca;
      ABCb.halfedge = ab;
      ABCc.halfedge = bc;
      ab.vertex = b;
      ab.face = ABC;
      ab.opposite = ba;
      ab.next = bc;
      bc.vertex = c;
      bc.face = ABC;
      bc.opposite = cb;
      bc.next = ca;
      ca.vertex = a;
      ca.face = ABC;
      ca.opposite = ac;
      ca.next = ab;

      ACD.halfedge = ac;
      ACDa.halfedge = da;
      ACDc.halfedge = ac;
      ACDd.halfedge = cd;
      ac.vertex = c;
      ac.face = ACD;
      ac.opposite = ca;
      ac.next = cd;
      cd.vertex = d;
      cd.face = ACD;
      cd.opposite = dc;
      cd.next = da;
      da.vertex = a;
      da.face = ACD;
      da.opposite = ad;
      da.next = ac;

      ADB.halfedge = ad;
      ADBa.halfedge = ba;
      ADBd.halfedge = ad;
      ADBb.halfedge = db;
      ad.vertex = d;
      ad.face = ADB;
      ad.opposite = da;
      ad.next = db;
      db.vertex = b;
      db.face = ADB;
      db.opposite = bd;
      db.next = ba;
      ba.vertex = a;
      ba.face = ADB;
      ba.opposite = ab;
      ba.next = ad;

      BDC.halfedge = bd;
      BDCb.halfedge = cb;
      BDCd.halfedge = bd;
      BDCc.halfedge = dc;
      bd.vertex = d;
      bd.face = BDC;
      bd.opposite = db;
      bd.next = dc;
      dc.vertex = c;
      dc.face = BDC;
      dc.opposite = cd;
      dc.next = cb;
      cb.vertex = b;
      cb.face = BDC;
      cb.opposite = bc;
      cb.next = bd;

      s_faceCache.Clear();
      s_faceCache.Add(ABC);
      s_faceCache.Add(ACD);
      s_faceCache.Add(ADB);
      s_faceCache.Add(BDC);
      mesh.AddHalfedgeStructure(ABC.halfedge);
    }

  }

}