import { useEffect, useRef, useState } from 'react';
import * as mapboxgl from 'mapbox-gl/esm';
import { tokens, Button } from '@fluentui/react-components';
import {
  ArrowResetRegular,
  FullScreenMaximizeRegular,
  FullScreenMinimizeRegular,
  PauseRegular,
  PlayRegular,
} from '@fluentui/react-icons';
import { MAPBOX_ACCESS_TOKEN } from '@modules/fleet/config/mapbox';
import type { PathPoint } from './tripEventUtils';

import 'mapbox-gl/dist/mapbox-gl.css';

const TRIP_PATH_SOURCE_ID = 'trip-path';
const TRIP_PATH_LAYER_ID = 'trip-path-layer';
const TRIP_ENDPOINTS_SOURCE_ID = 'trip-endpoints';
const TRIP_ENDPOINTS_LAYER_ID = 'trip-endpoints-layer';
const REPLAY_INTERVAL_MS = 250;

interface TripReplayMapProps {
  pathPoints: PathPoint[];
  isFullscreen?: boolean;
  onToggleFullscreen?: () => void;
}

export function TripReplayMap({
  pathPoints,
  isFullscreen = false,
  onToggleFullscreen,
}: TripReplayMapProps) {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const replayMarkerRef = useRef<mapboxgl.Marker | null>(null);
  const mapLoadedRef = useRef(false);
  const pathPointsRef = useRef(pathPoints);
  const [playheadIndex, setPlayheadIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);

  pathPointsRef.current = pathPoints;

  useEffect(() => {
    setPlayheadIndex(0);
    setIsPlaying(false);
  }, [pathPoints]);

  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) {
      return;
    }

    const map = new mapboxgl.Map({
      accessToken: MAPBOX_ACCESS_TOKEN,
      container: mapContainerRef.current,
      center: [28.1005628, -25.894096],
      zoom: 12,
      style: 'mapbox://styles/mapbox/standard',
    });

    map.addControl(new mapboxgl.NavigationControl(), 'top-right');

    map.on('load', () => {
      map.addSource(TRIP_PATH_SOURCE_ID, {
        type: 'geojson',
        data: {
          type: 'FeatureCollection',
          features: [],
        },
      });

      map.addLayer({
        id: TRIP_PATH_LAYER_ID,
        type: 'line',
        source: TRIP_PATH_SOURCE_ID,
        paint: {
          'line-color': '#2563eb',
          'line-width': 4,
          'line-opacity': 0.85,
        },
      });

      map.addSource(TRIP_ENDPOINTS_SOURCE_ID, {
        type: 'geojson',
        data: {
          type: 'FeatureCollection',
          features: [],
        },
      });

      map.addLayer({
        id: TRIP_ENDPOINTS_LAYER_ID,
        type: 'circle',
        source: TRIP_ENDPOINTS_SOURCE_ID,
        paint: {
          'circle-radius': 7,
          'circle-color': ['get', 'color'],
          'circle-stroke-width': 2,
          'circle-stroke-color': '#ffffff',
        },
      });

      const markerElement = document.createElement('div');
      markerElement.className = 'trip-replay-marker';
      markerElement.innerHTML = `
        <div style="
          width: 28px;
          height: 28px;
          border-radius: 50%;
          background: #1d4ed8;
          border: 2px solid #ffffff;
          box-shadow: 0 2px 6px rgba(0,0,0,0.25);
          display: flex;
          align-items: center;
          justify-content: center;
          color: #ffffff;
          font-size: 14px;
          transform-origin: center center;
        ">▲</div>
      `;

      replayMarkerRef.current = new mapboxgl.Marker({ element: markerElement })
        .setLngLat([28.1005628, -25.894096])
        .addTo(map);

      mapLoadedRef.current = true;
      updateTripPath(map, pathPointsRef.current, replayMarkerRef.current, 0);
    });

    mapRef.current = map;

    return () => {
      mapLoadedRef.current = false;
      replayMarkerRef.current?.remove();
      replayMarkerRef.current = null;
      map.remove();
      mapRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !mapLoadedRef.current) {
      return;
    }

    updateTripPath(map, pathPoints, replayMarkerRef.current, playheadIndex);
  }, [pathPoints, playheadIndex]);

  useEffect(() => {
    if (!isPlaying || pathPoints.length === 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setPlayheadIndex((currentIndex) => {
        if (currentIndex >= pathPoints.length - 1) {
          setIsPlaying(false);
          return currentIndex;
        }

        return currentIndex + 1;
      });
    }, REPLAY_INTERVAL_MS);

    return () => window.clearInterval(timer);
  }, [isPlaying, pathPoints.length]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !mapLoadedRef.current) {
      return;
    }

    const resizeMap = () => map.resize();
    const frame = window.requestAnimationFrame(resizeMap);
    window.addEventListener('resize', resizeMap);

    return () => {
      window.cancelAnimationFrame(frame);
      window.removeEventListener('resize', resizeMap);
    };
  }, [isFullscreen]);

  const currentPoint = pathPoints[playheadIndex];
  const canReplay = pathPoints.length > 1;

  return (
    <div className={`relative w-full overflow-hidden ${isFullscreen ? 'min-h-0 flex-1' : 'h-[49vh]'}`}>
      <div ref={mapContainerRef} className="h-full w-full" />

      <div className="absolute bottom-3 left-3 flex items-center gap-2 rounded-md px-2 py-1.5 shadow-sm" style={{ backgroundColor: `color-mix(in srgb, ${tokens.colorNeutralBackground1} 95%, transparent)` }}>
        <Button
          appearance="primary"
          size="small"
          icon={isPlaying ? <PauseRegular /> : <PlayRegular />}
          disabled={!canReplay}
          onClick={() => setIsPlaying((playing) => !playing)}
        >
          {isPlaying ? 'Pause' : 'Play'}
        </Button>
        <Button
          appearance="secondary"
          size="small"
          icon={<ArrowResetRegular />}
          disabled={!canReplay || playheadIndex === 0}
          onClick={() => {
            setIsPlaying(false);
            setPlayheadIndex(0);
          }}
        >
          Reset
        </Button>
        {currentPoint && (
          <span className="text-xs text-neutral-foreground-2">
            {currentPoint.speed != null ? `${Math.round(currentPoint.speed)} km/h` : '—'}
          </span>
        )}

         {onToggleFullscreen && (
          <Button
            appearance="subtle"
            size="small"
            className="shadow-sm"
            style={{ backgroundColor: `color-mix(in srgb, ${tokens.colorNeutralBackground1} 95%, transparent)` }}
            aria-label={isFullscreen ? 'Exit full screen' : 'Enter full screen'}
            icon={isFullscreen ? <FullScreenMinimizeRegular /> : <FullScreenMaximizeRegular />}
            onClick={onToggleFullscreen}
          />
        )}
      </div>
    </div>
  );
}

function updateTripPath(
  map: mapboxgl.Map,
  pathPoints: PathPoint[],
  replayMarker: mapboxgl.Marker | null,
  playheadIndex: number,
) {
  const coordinates = pathPoints.map((point) => [point.longitude, point.latitude] as [number, number]);

  const pathSource = map.getSource(TRIP_PATH_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
  pathSource?.setData({
    type: 'FeatureCollection',
    features: coordinates.length >= 2
      ? [{
          type: 'Feature',
          geometry: {
            type: 'LineString',
            coordinates,
          },
          properties: {},
        }]
      : [],
  });

  const endpointFeatures = [];
  if (pathPoints.length > 0) {
    endpointFeatures.push({
      type: 'Feature' as const,
      geometry: {
        type: 'Point' as const,
        coordinates: [pathPoints[0].longitude, pathPoints[0].latitude],
      },
      properties: { color: '#16a34a' },
    });

    if (pathPoints.length > 1) {
      const lastPoint = pathPoints[pathPoints.length - 1];
      endpointFeatures.push({
        type: 'Feature' as const,
        geometry: {
          type: 'Point' as const,
          coordinates: [lastPoint.longitude, lastPoint.latitude],
        },
        properties: { color: '#dc2626' },
      });
    }
  }

  const endpointsSource = map.getSource(TRIP_ENDPOINTS_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
  endpointsSource?.setData({
    type: 'FeatureCollection',
    features: endpointFeatures,
  });

  if (pathPoints.length === 0) {
    return;
  }

  const activePoint = pathPoints[Math.min(playheadIndex, pathPoints.length - 1)];
  replayMarker?.setLngLat([activePoint.longitude, activePoint.latitude]);

  const markerElement = replayMarker?.getElement().firstElementChild as HTMLElement | null;
  if (markerElement && activePoint.bearing != null) {
    markerElement.style.transform = `rotate(${activePoint.bearing}deg)`;
  }

  if (coordinates.length === 1) {
    map.flyTo({ center: coordinates[0], zoom: 15, duration: 600 });
    return;
  }

  const bounds = new mapboxgl.LngLatBounds();
  for (const coordinate of coordinates) {
    bounds.extend(coordinate);
  }

  map.fitBounds(bounds, {
    padding: 48,
    maxZoom: 16,
    duration: 600,
  });
}
