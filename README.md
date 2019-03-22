# Parsing Shenanigans

This repo is me experimenting with creating optimized parsers for .NET's new memory and I/O APIs:

* Span
* Memory
* ReadOnlySequence
* Pipelines (soon)

This consists of a pull parser (think `XmlReader`) for CSV that is more complex than an `IndexOf` but small enough to be easily re-written and optimized for each model. Each parser has not been fully tested for all inputs, so we should not place a huge importance, yet, on results of the benchmarks.

Current results:

|         Method |     Mean |    Error |   StdDev | Rank |
|--------------- |---------:|---------:|---------:|-----:|
|           Span |  2.231 s | 0.0087 s | 0.0077 s |    1 |
|         Memory |  3.606 s | 0.0393 s | 0.0368 s |    2 |
|       Sequence | 16.760 s | 0.0340 s | 0.0301 s |    3 |
| SequenceReader | 18.960 s | 0.0456 s | 0.0426 s |    4 |