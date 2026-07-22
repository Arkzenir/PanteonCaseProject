using CaseGame.Units;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class ProjectileTests
    {
        [Test]
        public void FacingRotation_TargetDirectlyToTheRight_FacesZeroDegrees()
        {
            var rotation = Projectile.FacingRotation(Vector3.zero, new Vector3(5f, 0f, 0f), forwardOffsetDegrees: 0f);

            Assert.AreEqual(0f, rotation.eulerAngles.z, 0.01f);
        }

        [Test]
        public void FacingRotation_TargetDirectlyAbove_FacesNinetyDegrees()
        {
            var rotation = Projectile.FacingRotation(Vector3.zero, new Vector3(0f, 5f, 0f), forwardOffsetDegrees: 0f);

            Assert.AreEqual(90f, rotation.eulerAngles.z, 0.01f);
        }

        [Test]
        public void FacingRotation_AppliesForwardOffset()
        {
            var rotation = Projectile.FacingRotation(Vector3.zero, new Vector3(5f, 0f, 0f), forwardOffsetDegrees: 90f);

            Assert.AreEqual(90f, rotation.eulerAngles.z, 0.01f);
        }

        [Test]
        public void FacingRotation_SamePositionAsTarget_ReturnsIdentity()
        {
            var rotation = Projectile.FacingRotation(new Vector3(2f, 3f, 0f), new Vector3(2f, 3f, 0f), forwardOffsetDegrees: 0f);

            Assert.AreEqual(Quaternion.identity, rotation);
        }
    }
}
