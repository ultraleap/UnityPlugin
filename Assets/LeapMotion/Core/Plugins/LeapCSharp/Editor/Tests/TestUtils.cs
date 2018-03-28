//using NUnit.Framework;
//using System;
//using System.Runtime.InteropServices;
//using Leap;
//using LeapInternal;
//using System.Collections.Generic;

//namespace Application
//{
//    public class TestUtils {

//        public const float StandardArmWidth = 150;
//        public const float StandardConfidence = .95f;
//        public const uint StandardHandFlags = 0;
//        public const float StandardGrabStrength = .75f;
//        public const UInt32 StandardRightHandID = 20;
//        public const UInt32 StandardLeftHandID = 30;
//        public const float StandardPinchStrength = .65f;
//        public const UInt64 StandardHandVisibleTime = 123456789;

//        public static Arm StandardArm() {
//            LEAP_BONE arm = makeArmStruct();
//            return TestHandFactory.makeArm(ref arm);
//        }

//        public static Hand StandardLeftHand(Frame frame) {
//            LEAP_HAND handStruct = makeLeftHandStruct();
//            Hand hand = TestHandFactory.makeHand(ref handStruct, frame);
//            return hand;
//        }

//        public static Frame StandardFrame(){
//            Frame frame = new Frame(1, 123456789, 240, new InteractionBox(Vector.Zero, new Vector(100,100,100)), new List<Hand>());
//            Hand hand = StandardLeftHand(frame);
//            frame.Hands.Add(hand);
//            return frame;
//        }

//        public static LEAP_HAND makeLeftHandStruct(){
//            LEAP_BONE arm = makeArmStruct();
//            IntPtr armPtr = IntPtr.Zero;
//            Marshal.StructureToPtr(arm, armPtr, false);
//            LEAP_HAND hand = new LEAP_HAND();
//            hand.arm = armPtr;
//            hand.confidence = StandardConfidence;
//            hand.flags = StandardHandFlags;
//            hand.grab_strength = StandardGrabStrength;
//            hand.id = StandardLeftHandID;
//            hand.pinch_strength = StandardPinchStrength;
//            hand.type = eLeapHandType.eLeapHandType_Left;
//            hand.visible_time = StandardHandVisibleTime;

//            LEAP_PALM palm = makePalmStruct();
//            hand.palm = StructToPtr<LEAP_PALM>(palm);

//            LEAP_DIGIT thumb = makeThumbStruct();
//            hand.thumb = StructToPtr<LEAP_DIGIT>(thumb);
//            LEAP_DIGIT middle = makeMiddleStruct();
//            hand.middle = StructToPtr<LEAP_DIGIT>(middle);
//            LEAP_DIGIT pinky = makePinkyStruct();
//            hand.pinky = StructToPtr<LEAP_DIGIT>(pinky);
//            LEAP_DIGIT ring = makeRingStruct();
//            hand.ring = StructToPtr<LEAP_DIGIT>(ring);
//            LEAP_DIGIT index = makeIndexStruct();
//            hand.index = StructToPtr<LEAP_DIGIT>(index);

//            return hand;
//        }

//        public static LEAP_BONE makeArmStruct(){
//            LEAP_BONE arm_bone = new LEAP_BONE();
//            arm_bone.rotation = new LEAP_QUATERNION(LeapQuaternion.Identity);
//            arm_bone.next_joint = new LEAP_VECTOR(Vector.Forward);
//            arm_bone.prev_joint = new LEAP_VECTOR(Vector.Zero);
//            arm_bone.width = StandardArmWidth;
//            return arm_bone;
//        }

//        public static LEAP_PALM makePalmStruct(){
//            LEAP_PALM palm = new LEAP_PALM();
//            palm.width = 90.9899978638f;
//            palm.velocity = setVector(-2.42819261551f, -1.20299720764f, -3.52022504807f);
//            palm.position = setVector(4.05205631256f, 418.420654297f, 60.3143463135f);
//            palm.stabilized_position = setVector(0.477336108685f, 425.246643066f, 65.9945526123f);
//            palm.direction = setVector(-0.112943358719f, 0.282659977674f, -0.952547729015f);
//            palm.normal = setVector(-0.0303287450224f, -0.959215939045f, -0.281042635441f);
//            //palm.basis = setBasis(0.993138492107f, 0.00285232067108f, -0.116909787059f, 0.0303287450224f, 0.959215939045f, 0.281042635441f, 0.112943358719f, -0.282659977674f, 0.952547729015f);

//            return palm;
//        }

//        public static LEAP_DIGIT makeThumbStruct(){
//            LEAP_DIGIT thumb = new LEAP_DIGIT();
//            thumb.tip_velocity  = setVector(-5.39079427719f, 0.810049712658f, -0.788471341133f);
//            thumb.stabilized_tip_position  = setVector(-63.4932022095f, 415.463562012f, 34.4805870056f);

//            LEAP_BONE metacarpal = new LEAP_BONE();
//            metacarpal.width =  20.25f;
//            metacarpal.prev_joint = setVector(-10.4010601044f, 395.914459229f, 115.632049561f);
//            metacarpal.next_joint = setVector(-10.4010601044f, 395.914459229f, 115.632049561f);
//            metacarpal.basis = setBasis(0.404724180698f, 0.914252579212f, 0.0184549689293f, -0.741630554199f, 0.316367000341f, 0.59152007103f, 0.534960210323f, -0.253089249134f, 0.806079030037f);
//            thumb.metacarpal = metacarpal;

//            LEAP_BONE proximal = new LEAP_BONE();
//            proximal.width =  20.25f;
//            proximal.prev_joint = setVector(-10.4010601044f, 395.914459229f, 115.632049561f);
//            proximal.next_joint = setVector(-40.2597351074f, 405.165710449f, 76.7136611938f);
//            proximal.basis = setBasis(0.356583654881f, 0.932824313641f, -0.0518350973725f, -0.717672348022f, 0.309017032385f, 0.624063253403f, 0.598159253597f, -0.18533013761f, 0.77965259552f);
//            thumb.proximal = proximal;

//            LEAP_BONE intermediate = new LEAP_BONE();
//            intermediate.width =  20.25f;
//            intermediate.prev_joint = setVector(-40.2597351074f, 405.165710449f, 76.7136611938f);
//            intermediate.next_joint = setVector(-51.4808959961f, 407.671478271f, 44.6151161194f);
//            intermediate.basis = setBasis(0.356583654881f, 0.932824313641f, -0.0518350973725f, -0.874377191067f, 0.352757155895f, 0.333206981421f, 0.329108774662f, -0.0734927356243f, 0.941427767277f);
//            thumb.intermediate = intermediate;

//            LEAP_BONE distal = new LEAP_BONE();
//            distal.width =  20.25f;
//            distal.prev_joint = setVector(-51.4808959961f, 407.671478271f, 44.6151161194f);
//            distal.next_joint = setVector(-64.6982879639f, 411.674041748f, 25.7204475403f);
//            distal.basis = setBasis(0.356583654881f, 0.932824313641f, -0.0518350973725f, -0.744241654873f, 0.317158699036f, 0.587804973125f, 0.564758718014f, -0.17102381587f, 0.807340323925f);
//            thumb.distal = distal;

//            return thumb;
//        }
//        public static LEAP_DIGIT makeIndexStruct(){
//            LEAP_DIGIT index = new LEAP_DIGIT();
//            index.tip_velocity  = setVector(-3.8000638485f, -5.70321321487f, -3.58872127533f);
//            index.stabilized_tip_position  = setVector(-34.3144950867f, 433.628448486f, -34.4959640503f);
//            index.metacarpal = new LEAP_BONE();
//            index.metacarpal.width =  19.3428001404f;
//            index.metacarpal.prev_joint = setVector(-3.00657176971f, 417.717956543f, 109.991752625f);
//            index.metacarpal.next_joint = setVector(-23.5701675415f, 427.488006592f, 40.033405304f);
//            index.metacarpal.basis = setBasis(0.960076153278f, 0.0502709820867f, -0.275184690952f, -0.011258774437f, 0.989867150784f, 0.141549706459f, 0.279512137175f, -0.132800251245f, 0.950913786888f);
//            index.proximal = new LEAP_BONE();
//            index.proximal.width =  19.3428001404f;
//            index.proximal.prev_joint = setVector(-23.5701675415f, 427.488006592f, 40.033405304f);
//            index.proximal.next_joint = setVector(-27.6382827759f, 431.891815186f, -2.50863146782f);
//            index.proximal.basis = setBasis(0.995396256447f, 0.0245693791658f, -0.0926422104239f, -0.0148328179494f, 0.994429171085f, 0.104358196259f, 0.0946901366115f, -0.102503612638f, 0.990215539932f);
//            index.intermediate = new LEAP_BONE();
//            index.intermediate.width =  19.3428001404f;
//            index.intermediate.prev_joint = setVector(-27.6382827759f, 431.891815186f, -2.50863146782f);
//            index.intermediate.next_joint = setVector(-29.8354091644f, 430.335083008f, -26.5285701752f);
//            index.intermediate.basis = setBasis(0.995396256447f, 0.0245693791658f, -0.0926422104239f, -0.0303832087666f, 0.997621238232f, -0.0618767589331f, 0.090901568532f, 0.0644066631794f, 0.993774950504f);
//            index.distal = new LEAP_BONE();
//            index.distal.width =  19.3428001404f;
//            index.distal.prev_joint = setVector(-29.8354091644f, 430.335083008f, -26.5285701752f);
//            index.distal.next_joint = setVector(-31.3065071106f, 426.996734619f, -43.2201576233f);
//            index.distal.basis = setBasis(0.995396256447f, 0.0245693791658f, -0.0926422104239f, -0.0421040803194f, 0.980417966843f, -0.192374363542f, 0.0861015692353f, 0.195389330387f, 0.976938843727f);            
//            return index;
//        }
//        public static LEAP_DIGIT makeMiddleStruct(){
//            LEAP_DIGIT middle = new LEAP_DIGIT();

//            middle.tip_velocity  = setVector(-4.81796693802f, -3.54747653008f, -4.46720075607f);
//            middle.stabilized_tip_position  = setVector(-5.33823776245f, 437.299865723f, -48.0671043396f);
//            middle.metacarpal = new LEAP_BONE();
//            middle.metacarpal.width =  18.9972000122f;
//            middle.metacarpal.prev_joint = setVector(8.50413608551f, 420.093566895f, 106.320571899f);
//            middle.metacarpal.next_joint = setVector(-1.64440250397f, 429.65411377f, 37.9599304199f);
//            middle.metacarpal.basis = setBasis(0.976770401001f, -0.137622743845f, -0.164254486561f, 0.157354965806f, 0.980959653854f, 0.113831415772f, 0.14546123147f, -0.137033417821f, 0.979828000069f);
//            middle = new LEAP_DIGIT();
//            middle.proximal.width =  18.9972000122f;
//            middle.proximal.prev_joint = setVector(-1.64440250397f, 429.65411377f, 37.9599304199f);
//            middle.proximal.next_joint = setVector(-0.884309947491f, 435.149047852f, -9.92019367218f);
//            middle.proximal.basis = setBasis(0.987419307232f, -0.15810456872f, -0.00246962672099f, 0.157335564494f, 0.980819284916f, 0.115061067045f, -0.0157694239169f, -0.114002078772f, 0.993355333805f);
//            middle.intermediate = new LEAP_BONE();
//            middle.intermediate.width =  18.9972000122f;
//            middle.intermediate.prev_joint = setVector(-0.884309947491f, 435.149047852f, -9.92019367218f);
//            middle.intermediate.next_joint = setVector(-1.23704957962f, 433.389343262f, -38.2999038696f);
//            middle.intermediate.basis = setBasis(0.987419307232f, -0.15810456872f, -0.00246962672099f, 0.157636553049f, 0.985481441021f, -0.0630642101169f, 0.0124045107514f, 0.0618815124035f, 0.998006403446f);
//            middle.distal = new LEAP_BONE();
//            middle.distal.width =  18.9972000122f;
//            middle.distal.prev_joint = setVector(-1.23704957962f, 433.389343262f, -38.2999038696f);
//            middle.distal.next_joint = setVector(-1.88998162746f, 429.598876953f, -56.6940689087f);
//            middle.distal.basis = setBasis(0.987419307232f, -0.15810456872f, -0.00246962672099f, 0.15425927937f, 0.966600954533f, -0.204662457108f, 0.0347452126443f, 0.201706692576f, 0.978829503059f);            
//            return middle;
//        }
//        public static LEAP_DIGIT makeRingStruct(){
//            LEAP_DIGIT ring = new LEAP_DIGIT();
//            ring.tip_velocity  = setVector(-3.33204102516f, -0.481686502695f, -4.47619199753f);
//            ring.stabilized_tip_position  = setVector(22.4790019989f, 438.9140625f, -41.785900116f);
//            ring.metacarpal = new LEAP_BONE();
//            ring.metacarpal.width =  18.0770397186f;
//            ring.metacarpal.prev_joint = setVector(20.467294693f, 419.406768799f, 103.611236572f);
//            ring.metacarpal.next_joint = setVector(20.7890357971f, 427.893066406f, 41.5495834351f);
//            ring.metacarpal.basis = setBasis(0.972869992256f, -0.229843020439f, -0.0263850279152f, 0.231295481324f, 0.963752150536f, 0.132982060313f, -0.00513637112454f, -0.135476991534f, 0.99076718092f);
//            ring.proximal = new LEAP_BONE();
//            ring.proximal.width =  18.0770397186f;
//            ring.proximal.prev_joint = setVector(20.7890357971f, 427.893066406f, 41.5495834351f);
//            ring.proximal.next_joint = setVector(23.8692779541f, 434.40512085f, -2.54544758797f);
//            ring.proximal.basis = setBasis(0.970832586288f, -0.237512439489f, 0.0327406525612f, 0.229632943869f, 0.960387766361f, 0.157873630524f, -0.0689406692982f, -0.145750537515f, 0.986916363239f);
//            ring.intermediate = new LEAP_BONE();
//            ring.intermediate.width =  18.0770397186f;
//            ring.intermediate.prev_joint = setVector(23.8692779541f, 434.40512085f, -2.54544758797f);
//            ring.intermediate.next_joint = setVector(24.6341648102f, 433.715576172f, -30.2282981873f);
//            ring.intermediate.basis = setBasis(0.970832586288f, -0.237512439489f, 0.0327406525612f, 0.238163232803f, 0.97106552124f, -0.0176078639925f, -0.027611233294f, 0.0248919092119f, 0.999308764935f);
//            ring.distal = new LEAP_BONE();
//            ring.distal.width =  18.0770397186f;
//            ring.distal.prev_joint = setVector(24.6341648102f, 433.715576172f, -30.2282981873f);
//            ring.distal.next_joint = setVector(24.5136470795f, 430.681640625f, -48.6639328003f);
//            ring.distal.basis = setBasis(0.970832586288f, -0.237512439489f, 0.0327406525612f, 0.239671647549f, 0.957716107368f, -0.159177169204f, 0.00645030476153f, 0.162381380796f, 0.986706972122f);            
//            return ring;
//        }

//        public static LEAP_DIGIT makePinkyStruct(){
//            LEAP_DIGIT pinky = new LEAP_DIGIT();
//            pinky.tip_velocity  = setVector(-3.13705062866f, -1.8955719471f, -4.04747152328f);
//            pinky.stabilized_tip_position  = setVector(59.6034851074f, 428.404846191f, -15.9961853027f);
//            pinky.metacarpal = new LEAP_BONE();
//            pinky.metacarpal.width =  16.0574398041f;
//            pinky.metacarpal.prev_joint = setVector(32.3069076538f, 411.609985352f, 102.349822998f);
//            pinky.metacarpal.next_joint = setVector(40.7679252625f, 421.499420166f, 45.8441238403f);
//            pinky.metacarpal.basis = setBasis(0.914057135582f, -0.40003734827f, 0.0668554678559f, 0.378428071737f, 0.900490164757f, 0.214265406132f, -0.145916849375f, -0.170550838113f, 0.974484801292f);
//            pinky.proximal = new LEAP_BONE();
//            pinky.proximal.width =  16.0574398041f;
//            pinky.proximal.prev_joint = setVector(40.7679252625f, 421.499420166f, 45.8441238403f);
//            pinky.proximal.next_joint = setVector(53.0596656799f, 423.73425293f, 12.7655563354f);
//            pinky.proximal.basis = setBasis(0.856013357639f, -0.428534567356f, 0.289135336876f, 0.382619917393f, 0.901311993599f, 0.203073069453f, -0.347624987364f, -0.063204318285f, 0.935500979424f);
//            pinky.intermediate = new LEAP_BONE();
//            pinky.intermediate.width =  16.0574398041f;
//            pinky.intermediate.prev_joint = setVector(53.0596656799f, 423.73425293f, 12.7655563354f);
//            pinky.intermediate.next_joint = setVector(58.6598167419f, 422.313201904f, -5.92041635513f);
//            pinky.intermediate.basis = setBasis(0.856013357639f, -0.428534567356f, 0.289135336876f, 0.430418163538f, 0.900599420071f, 0.0605055578053f, -0.286323845387f, 0.0726555287838f, 0.955374181271f);
//            pinky.distal = new LEAP_BONE();
//            pinky.distal.width =  16.0574398041f;
//            pinky.distal.prev_joint = setVector(58.6598167419f, 422.313201904f, -5.92041635513f);
//            pinky.distal.next_joint = setVector(62.670501709f, 419.209625244f, -22.3943195343f);
//            pinky.distal.basis = setBasis(0.856013357639f, -0.428534567356f, 0.289135336876f, 0.461628049612f, 0.885402917862f, -0.0544174276292f, -0.232681512833f, 0.18005502224f, 0.955740272999f);
//            return pinky;
//        }

////        public Int32 finger_id;
////        public LEAP_BONE metacarpal;
////        public LEAP_BONE proximal;
////        public LEAP_BONE intermediate;
////        public LEAP_BONE distal;
////        public LEAP_VECTOR tip_velocity;
////        public LEAP_VECTOR stabilized_tip_position;
////        public bool is_extended;

//        private static LEAP_VECTOR setVector(float x, float y, float z){
//            LEAP_VECTOR result = new LEAP_VECTOR();
//            result.x = x;
//            result.y = y;
//            result.z = z;
//            return result;
//        }
//        private static LEAP_MATRIX setBasis(float xx, float xy, float xz,
//                                            float yx, float yy, float yz,
//                                            float zx, float zy, float zz){
//            LEAP_MATRIX basis = new LEAP_MATRIX();
//            basis.x_basis = setVector(xx, xy, xz);
//            basis.y_basis = setVector(yx, yy, yz);
//            basis.z_basis = setVector(zx, zy, zz);
//            return basis;

//        }
//        private static LEAP_MATRIX identityBasis(){
//            LEAP_MATRIX identity = new LEAP_MATRIX();
//            identity.x_basis = setVector(1f, 0f, 0f);
//            identity.y_basis = setVector(0f, 1f, 0f);
//            identity.z_basis = setVector(0f, 0f, 1f);
//            return identity;
//        } 

//        public static IntPtr StructToPtr<T>(T structure) where T: struct{
//            IntPtr ptr = IntPtr.Zero;
//            Marshal.StructureToPtr(structure, ptr, false);
//            return ptr;
//        }


//    }
//}

