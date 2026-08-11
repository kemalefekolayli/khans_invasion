# Known Issues

## Border collision hardening

- Irregular province boundaries can let a selected general overlap a neighboring province collider even when supply preflight rejects the transition.
- The supply route is not paid or committed in this case, but repeated `SUP ENTER` / `SUP LOW` attempts can occur while movement input is held.
- Future fix: restore the last safe physical position and suppress repeated attempts until the movement direction changes or the transition becomes valid.
