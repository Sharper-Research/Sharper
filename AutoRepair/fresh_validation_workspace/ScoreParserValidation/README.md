# ScoreParserValidation

Fresh AutoRepair validation repository created outside the 13-repository benchmark.

It contains one real crash path and one intentional validator:

- `ParseScore("alice,not-a-number")` throws `FormatException` before repair and should become an AutoPatchable candidate.
- `ParseScore(null!)` throws `ArgumentNullException` intentionally and should be classified as `IntentionalValidator`, not patched.
