using BenchmarkDotNet.Running;
using FlyReservation.Benchmarks;

// Always in Release, without a debugger (Docker must be running):
//   dotnet run -c Release --project benchmarks/FlyReservation.Benchmarks
BenchmarkRunner.Run<FlightSearchBenchmarks>(args: args);
