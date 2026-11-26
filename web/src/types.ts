// One flight in the GET /api/flights response: the same fields as src/FlyReservation.Api/Dtos/FlightResponse.cs.
// JSON field names arrive in camelCase
export type FlightResponse = {
    id: number
    flightNumber: string
    origin: string
    destination: string
    departureTime: string // JSON has no date type: it arrives as text like "2026-12-01T08:00:00Z"
    arrivalTime: string
    totalSeats: number
    availableSeats: number
    price: number // decimal in C#; JavaScript has a single number type
}

// The shape of every paged list the API returns: src/FlyReservation.Api/Dtos/PagedResponse.cs
export type PagedResponse<T> = {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
}
