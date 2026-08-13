# Army System Roadmap

## Confirmed direction

- Defeated enemy armies are captured instead of automatically returning to their capital.
- Captured enemy generals remain available as prisoners and can later support a headhunter/collection system.
- Returning captured generals to the player capital can award charisma.
- The prisoner collection should eventually be visible in a dedicated UI tab or panel.

## Implementation order

1. **Restore capture flow**
   - After winning an army battle, transfer the defeated general/army into a captured state.
   - Prevent captured armies from continuing normal movement or AI behavior.
   - Preserve the existing battle result and loot behavior.

2. **Army force limit**
   - Add a nation-level maximum army capacity.
   - Make the limit inspector-tunable and derive its base value from population/economy or buildings later.
   - Use a soft warning/block when the player tries to create another army above the limit.

3. **Army upkeep**
   - Each active army consumes treasury gold per turn.
   - Upkeep scales with army size, with a clear minimum cost per army.
   - Captured, destroyed, and disbanded armies stop consuming upkeep immediately.

4. **Reinforcement cost**
   - Replacing battle losses costs gold and population/available recruits.
   - Larger losses should create a meaningful economic recovery period.

5. **General recruitment from the barracks**
   - Add a new barracks button beside the existing recruit-army action.
   - General creation requires gold and configurable conditions.
   - Enforce a general/army limit so generals cannot be produced indefinitely.
   - The first general remains the only starting general; later generals are unlocked by quest or recruitment conditions.

6. **Prisoner/headhunter collection**
   - Add a prisoner list UI showing captured generals.
   - Use captured-general count and/or returned prisoners as a charisma source.
   - Decide later whether prisoners can be recruited, exchanged, ransomed, or only retained for charisma.

## Next step

Before implementation, inspect the current battle-resolution and captured-state code paths. Restore the capture regression first, then define the exact force-limit and upkeep formulas in the inspector before adding the barracks general button.

