import type { FlightResponse, PagedResponse } from './types'

// Every call goes to /api/... on the page's own origin. In development, Vite's proxy forwards it to the .NET API
export async function getFlights(): Promise<PagedResponse<FlightResponse>> {
  const response = await fetch('/api/flights')

  // fetch only fails when no response arrives at all. A 400 or 500 is still a response, so check the status here
  if (!response.ok) {
    throw new Error(`GET /api/flights failed with ${response.status}`)
  }

  return response.json()
}
