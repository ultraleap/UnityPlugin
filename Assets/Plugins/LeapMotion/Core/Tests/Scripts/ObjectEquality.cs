/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;

namespace Leap.LeapCSharp.Tests {
  [TestFixture()]
  public class ObjectEquality {
    [Test()]
    public void Vector_ints() {
      Vector thisVector = new Leap.Vector(1, 2, 3);
      Vector thatVector = new Leap.Vector(1, 2, 3);
      Assert.True(thisVector.Equals(thatVector), "this.Equals(that) Vector");
      //Assert.True (thisVector == thatVector, "this == that Vector");

    }
    [Test()]
    public void Vector_floats() {
      Vector thisVector = new Leap.Vector(1.111111111111111f, 2.222222222222222f, 3.333333333333333f);
      Vector thatVector = new Leap.Vector(1.111111111111111f, 2.222222222222222f, 3.333333333333333f);
      Assert.True(thisVector.Equals(thatVector), "this.Equals(that) Vector");
      //Assert.True (thisVector == thatVector, "this == that Vector");

    }
    [Test()]
    public void Vector_more_floats() {
      Vector thisVector = new Vector(0.199821f, -0.845375f, 0.495392f);
      Vector thatVector = new Vector(0.199821f, -0.845375f, 0.495392f);
      Assert.True(thisVector.Equals(thatVector), "this.Equals(that) Vector");
      //Assert.True (thisVector == thatVector, "this == that Vector");
    }
  }
}

