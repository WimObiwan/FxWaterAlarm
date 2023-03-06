using Core.Entities;
using Core.Repositories;

namespace Core.Queries;

public interface ILastMeasurementQuery
{
    Task<Measurement?> Get(string devEui);
}

public class LastMeasurementQuery : ILastMeasurementQuery
{
    private readonly IMeasurementRepository _measurementRepository;

    public LastMeasurementQuery(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<Measurement?> Get(string devEui)
    {
        return await _measurementRepository.GetLast(devEui);
    }
}