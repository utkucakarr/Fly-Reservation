import type { FlightResponse } from './types'

// Temporary: hand-written flights until the app reads them from the API
export const sampleFlights: FlightResponse[] = [
    { id: 1, flightNumber: 'TK101', origin: 'Istanbul', destination: 'Ankara', departureTime: '2026-12-01T08:00:00Z', arrivalTime: '2026-12-01T09:10:00Z', totalSeats: 180, availableSeats: 3, price: 900 },
    { id: 2, flightNumber: 'TK202', origin: 'Istanbul', destination: 'Izmir', departureTime: '2026-12-01T10:00:00Z', arrivalTime: '2026-12-01T11:15:00Z', totalSeats: 180, availableSeats: 5, price: 1200 },
    { id: 3, flightNumber: 'PC303', origin: 'Ankara', destination: 'Antalya', departureTime: '2026-12-02T07:30:00Z', arrivalTime: '2026-12-02T08:45:00Z', totalSeats: 150, availableSeats: 1, price: 1500 },
]