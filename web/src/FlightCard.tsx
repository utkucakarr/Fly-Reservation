import type { FlightResponse } from './types'

type FlightCardProps = {
    flight: FlightResponse
    onReserve: (id: number) => void
}

// One flight's card. It gets its data (props) from outside and never changes it:
// the button only reports the click (onReserve), and App changes the data
export function FlightCard({ flight, onReserve }: FlightCardProps) {
    return (
        <div className="card">
            <strong>{flight.flightNumber}</strong> {flight.origin} → {flight.destination} · {flight.price} TL · {flight.availableSeats} koltuk
            <br />
            <input placeholder="Yolcu adı" />
            <button disabled={flight.availableSeats === 0} onClick={() => onReserve(flight.id)}>
                Koltuk ayır
            </button>
        </div>
    )
}