// HandData.cginc

// ==============
// Fingertip Data
// ==============

uniform float4 _Leap_LH_Fingertips[5];
uniform float4 _Leap_RH_Fingertips[5];

uniform float4 _Leap_LH_PalmTriangles[15];
uniform float4 _Leap_RH_PalmTriangles[15];

uniform float4 _Leap_LH_FingerSegments[28];
uniform float4 _Leap_RH_FingerSegments[28];

// =========
// Functions
// =========

#define IDENTITY_MATRIX4x4 { 1.0, 0.0, 0.0, 0.0, \
                             0.0, 1.0, 0.0, 0.0, \
                             0.0, 0.0, 1.0, 0.0, \
                             0.0, 0.0, 0.0, 1.0 }

float4x4 Leap_HandData_Preprocess_Matrix = IDENTITY_MATRIX4x4;

void Leap_ClearPreprocessMatrix() {
  //Leap_HandData_Preprocess_Matrix = IDENTITY_MATRIX4x4;
}

//----------------------
// Distance Support (iq)
//----------------------
// http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

float Leap_SqrLength(float3 v) {
  return v.x * v.x + v.y * v.y + v.z * v.z;
}

float Leap_SqrDistLine(float3 p, float3 a, float3 b) {
  float3 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return Leap_SqrLength( pa - ba*h );
}

float Leap_SqrDistLine_WithThickness(float3 p, float3 a, float3 b, float approxR) {
  return max(Leap_SqrDistLine(p, a, b) - approxR * approxR, 0);
}

float dot2(float3 v) { return dot(v,v); }
float Leap_SqrDistTriangle(float3 p, float3 a, float3 b, float3 c) {
  float3 ba = b - a; float3 pa = p - a;
  float3 cb = c - b; float3 pb = p - b;
  float3 ac = a - c; float3 pc = p - c;
  float3 nor = cross( ba, ac );
  
  if (sign(dot(cross(ba,nor),pa))
          + sign(dot(cross(cb,nor),pb))
          + sign(dot(cross(ac,nor),pc)) < 2.0) {
    return min( min(
            dot2(ba*clamp(dot(ba,pa)/dot2(ba),0.0,1.0)-pa),
            dot2(cb*clamp(dot(cb,pb)/dot2(cb),0.0,1.0)-pb) ),
            dot2(ac*clamp(dot(ac,pc)/dot2(ac),0.0,1.0)-pc) );
  }
  else {
    return dot(nor,pa)*dot(nor,pa)/dot2(nor);
  }
}

float Leap_SqrDistTriangle_WithThickness(float3 p, float3 a, float3 b, float3 c, float approxR) {
  return max(Leap_SqrDistTriangle(p, a, b, c) - approxR * approxR, 0);
}

float IQ_sdCapsule(float3 p, float3 a, float3 b, float r) {
  float3 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return length( pa - ba*h ) - r;
}

float IQ_udTriangle( float3 p, float3 a, float3 b, float3 c ) {
  float3 ba = b - a; float3 pa = p - a;
  float3 cb = c - b; float3 pb = p - b;
  float3 ac = a - c; float3 pc = p - c;
  float3 nor = cross( ba, ac );

  return sqrt(
  (sign(dot(cross(ba,nor),pa)) +
    sign(dot(cross(cb,nor),pb)) +
    sign(dot(cross(ac,nor),pc))<2.0)
    ?
    min( min(
    dot2(ba*clamp(dot(ba,pa)/dot2(ba),0.0,1.0)-pa),
    dot2(cb*clamp(dot(cb,pb)/dot2(cb),0.0,1.0)-pb) ),
    dot2(ac*clamp(dot(ac,pc)/dot2(ac),0.0,1.0)-pc) )
    :
    dot(nor,pa)*dot(nor,pa)/dot2(nor) );
}

// --------
// Distance
// --------

float Leap_Map(float input, float valueMin, float valueMax, float resultMin, float resultMax) {
  if (valueMin == valueMax) return resultMin;
  return lerp(resultMin, resultMax, saturate((input - valueMin) / (valueMax - valueMin)));
}

float Leap_Map4(float input, float4 mapValues) {
  return Leap_Map(input, mapValues.x, mapValues.y, mapValues.z, mapValues.w);
}

float Leap_Dist(float3 a, float3 b) {
  float3 ab = b - a;
  return sqrt(ab.x * ab.x + ab.y * ab.y + ab.z * ab.z);
}

float Leap_SqrDist(float3 a, float3 b) {
  float3 ab = b - a;
  return ab.x * ab.x + ab.y * ab.y + ab.z * ab.z;
}

float Leap_DistanceToFingertips(float3 pos) {
	float dist =     Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[0]));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[1])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[2])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[3])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[4])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[0])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[1])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[2])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[3])));
	dist = min(dist, Leap_Dist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[4])));
	return dist;
}

float Leap_SqrDistToFingertips(float3 pos) {
	float dist =     Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[0]));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[1])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[2])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[3])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[4])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[0])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[1])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[2])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[3])));
	dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[4])));
	return dist;
}

float Leap_SqrDistToFingertips_WithScale(float3 pos, float3 scale) {
	float dist =     Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[0]) * scale);
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[1]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[2]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[3]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[4]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[0]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[1]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[2]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[3]) * scale));
	dist = min(dist, Leap_SqrDist(pos * scale, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[4]) * scale));
	return dist;
}

float Leap_SqrDistToHand(float3 pos) {
  // Fingertips
	//float dist =     Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[0]));
  //for (int i = 1; i < 5; i++) {
	//  dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[i])));
  //}
  //for (int j = 0; j < 5; j++) {
	//  dist = min(dist, Leap_SqrDist(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[j])));
  //}
  float dist = 1000000000;

  float handThickness = 0.00;

  // Palm Triangles
  for (int k = 0; k < 15; k += 3) {
	  dist = min(dist, Leap_SqrDistTriangle_WithThickness(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_PalmTriangles[k + 0]).xyz,
                                                             mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_PalmTriangles[k + 1]).xyz,
                                                             mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_PalmTriangles[k + 2]).xyz,
                                                        (k < 3 ? handThickness : handThickness * 1.4)));
  }
  for (int l = 0; l < 15; l += 3) {
	  dist = min(dist, Leap_SqrDistTriangle_WithThickness(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_PalmTriangles[l + 0]).xyz,
                                                             mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_PalmTriangles[l + 1]).xyz,
                                                             mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_PalmTriangles[l + 2]).xyz,
                                                        (l < 3 ? handThickness : handThickness * 1.4)));
  }

  // Finger Segments
  for (int m = 0; m < 28; m += 2) {
	  dist = min(dist, Leap_SqrDistLine_WithThickness(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_FingerSegments[m + 0]).xyz,
                                                         mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_FingerSegments[m + 1]).xyz,
                                                    handThickness));
  }
  for (int n = 0; n < 28; n += 2) {
	  dist = min(dist, Leap_SqrDistLine_WithThickness(pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_FingerSegments[n + 0]).xyz,
                                                         mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_FingerSegments[n + 1]).xyz,
                                                    handThickness));
  }

	return dist;
}

// ------
// Planes
// ------

//float3 Leap_FingertipsDepthInPlane_AssumeHandsInLocalPlaneSpace() {
//  float3 closest = _Leap_LH_Fingertips[0];
//  for (int i = 1; i < 5; i++) {
//    if (_leap_LH_Fingertips[i].z < )
//  }
//}

// Copied-- plane displacement shader
// TODO: Rename, potentially refactor

// Uses dot product to get the component distance along the normal.
// Also sets the parallel and perpendicular vectors.
float distanceToPlane(float3 normal, float4 point1, float4 point2, out float3 perp, out float3 para) {
  float3 diff = (point2 - point1).xyz;
  float normalDotDiff = dot( normal , diff );
  perp = normal * normalDotDiff;
  para = diff - perp;
  return normalDotDiff;
}

// Calculates the displacement of a point by a finger using:
//   - The signed distance (negative is past plane) of the finger relative to the surface of the plane
//   - The "finger influence" of the finger, which is a function of the distance between the point 
//      and the finger along the plane.
//
// The baseDisplacement argument sets the maxiumum distance from the plane that will be reported, allowing
// for the smooth falloff of finger influence.
float getFingerDisplacement( float3 normal, float4 position , float4 fingerPosition, float baseDisplacement, float distanceCutoff) {
  float maxDisplacement = baseDisplacement; // What is is max distance forward we'll report
  float3 para = float3(0.,0.,0.); // Vector pointing parallell to the plane
  float3 perp = float3(0.,0.,0.); // Vector pointing along the normal of the plane
  float d = distanceToPlane( normal , position , fingerPosition , perp , para ); // How far is the finger tip from the plane.
  float diff = min(0, d - maxDisplacement);
  float len = length( para ); // How far is the point from the tip, along the plane
  float fingerInfluence = 0.0;
  if( len <= distanceCutoff) {
    fingerInfluence = pow((1 - (len / distanceCutoff)), 3.);
  }
 
  float displacement = maxDisplacement + (diff * fingerInfluence);
  return displacement;
}

// Return of the greatest displacement caused to the point from a finger. 
float getMinDisplacement(float3 normal, float4 pos, float baseDisplacement, float distanceCutoff) {
  float zDisplacement = baseDisplacement; // 100 units seems a reasonable maxiumum distance.
 
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[0]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[1]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[2]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[3]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_LH_Fingertips[4]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[0]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[1]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[2]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[3]), baseDisplacement, distanceCutoff ));
  zDisplacement = min(zDisplacement, getFingerDisplacement( normal, pos, mul(Leap_HandData_Preprocess_Matrix, _Leap_RH_Fingertips[4]), baseDisplacement, distanceCutoff ));
  return zDisplacement;
}

// Get the displacement past the plane (using 0 as base displacement)
float getDisplacement( float3 normal , float4 pos ){
  float pushDist = min(0, getMinDisplacement(normal, pos, 0.0, 0.18));
  float toReturn = clamp(pushDist, -.06, 0. );
  return toReturn;
}

float _normalDisplacementMagnitude = 100.0;



// =================
// Virtual Materials
// =================

// -------------------------------
// Plath (Color by Hand Proximity)
// -------------------------------

float4 Leap_EvalGradientWithMap(float input, sampler2D gradient, float4 inputMapping) {
  float eval = Leap_Map(input,   inputMapping.x,
                                 inputMapping.y,
                                 inputMapping.z,
                                 inputMapping.w);
  return tex2D(gradient, float2(eval, 0));
}

float4 evalProximityColor(float3 worldPos, sampler2D proximityGradient, float4 proximityMapping) {
  float sqrDist = Leap_SqrDistToHand(worldPos);
  float eval = saturate(Leap_Map(sqrDist, proximityMapping.x * proximityMapping.x,
                                          proximityMapping.y * proximityMapping.y,
                                          proximityMapping.z,  proximityMapping.w));
  return tex2D(proximityGradient, float2(eval, 0));
}

float4 evalProximityColorLOD(float3 worldPos, sampler2D proximityGradient, float4 proximityMapping, int lod) {
  float sqrDist = Leap_SqrDistToHand(worldPos);
  float eval = saturate(Leap_Map(sqrDist, proximityMapping.x * proximityMapping.x,
                                          proximityMapping.y * proximityMapping.y,
                                          proximityMapping.z, proximityMapping.w));
  return tex2Dlod(proximityGradient, float4(eval, 0, 0, lod));
}
