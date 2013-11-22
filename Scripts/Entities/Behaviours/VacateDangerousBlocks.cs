﻿using System.Linq;

using Zombles.Entities;
using Zombles.Geometry;

namespace Zombles.Scripts.Entities.Behaviours
{
    public class VacateDangerousBlocks : SubsumptionStack.Layer
    {
        protected RouteNavigation RouteNavigation { get; private set; }

        private Intersection _curDest;

        protected override void OnSpawn()
        {
            RouteNavigation = Entity.GetComponentOrNull<RouteNavigation>();
            _curDest = null;
        }

        protected override bool OnThink(double dt)
        {
            if (RouteNavigation == null) return false;

            var vacating = _curDest != null && RouteNavigation.HasRoute && RouteNavigation.CurrentTarget == _curDest.Position;

            var block = World.GetBlock(Position2D);

            if (!World.GetTile(Position2D).IsInterior) {
                if (vacating) RouteNavigation.CurrentRoute = null;
                return false;
            }

            var zoms = SearchNearbyVisibleEnts(8f, (ent, diff) =>
                ent.HasComponent<Zombie>() &&
                ent.GetComponent<Health>().IsAlive &&
                diff.LengthSquared < 3f * 3f &&
                World.GetTile(ent.Position2D).IsInterior);
            
            var danger = zoms.Count() > 0;

            if (!danger) {
                if (vacating) RouteNavigation.CurrentRoute = null;
                return false;
            }

            if (RouteNavigation.HasRoute && RouteNavigation.CurrentTarget == _curDest.Position) {
                Human.StartMoving(World.Difference(Position2D, RouteNavigation.NextWaypoint));
                return true;
            }

            _curDest = World.GetIntersections(block)
                .OrderBy(x => World.Difference(Entity.Position2D, x.Position).LengthSquared)
                .First();

            RouteNavigation.NavigateTo(_curDest.Position);
            return false;
        }

        protected override void OnRemove()
        {
            RouteNavigation = null;
        }
    }
}
