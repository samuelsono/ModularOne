import { useEffect, useRef, useCallback } from 'react';
import type { CSSProperties } from 'react';
import * as mapboxgl from 'mapbox-gl/esm';
import { MAPBOX_ACCESS_TOKEN } from '@modules/fleet/config/mapbox';
import type { Vehicle } from '@modules/fleet/types/vehicle';
import { getVehicleCoordinates, vehicleHasLocation } from '@modules/fleet/types/vehicle';
import { getVehicleDisplayName } from '@modules/fleet/services/vehicleService';

import 'mapbox-gl/dist/mapbox-gl.css';

const VEHICLE_SOURCE_ID = 'fleet-vehicles';
const VEHICLE_LAYER_ID = 'fleet-vehicles-layer';
const DEFAULT_CENTER: [number, number] = [28.1005628, -25.894096];
const DEFAULT_ZOOM = 6;

interface AppMapProps {
  vehicles?: Vehicle[];
  className?: string;
  style?: CSSProperties;
  /** Bumps resize logic when layout changes (e.g. dialog open). */
  layoutKey?: string | number | boolean;
}

const AppMap = ({ vehicles = [], className, style, layoutKey }: AppMapProps) => {
  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const mapLoadedRef = useRef(false);
  const vehiclesRef = useRef(vehicles);

  vehiclesRef.current = vehicles;

  const resizeMap = useCallback(() => {
    mapRef.current?.resize();
  }, []);

  useEffect(() => {
    if (!mapContainerRef.current || mapRef.current) {
      return;
    }

    const map = new mapboxgl.Map({
      accessToken: MAPBOX_ACCESS_TOKEN,
      container: mapContainerRef.current,
      center: DEFAULT_CENTER,
      zoom: DEFAULT_ZOOM,
      style: 'mapbox://styles/mapbox/standard',
    });

    map.addControl(new mapboxgl.NavigationControl(), 'top-right');

    map.on('load', () => {
      map.addSource(VEHICLE_SOURCE_ID, {
        type: 'geojson',
        data: {
          type: 'FeatureCollection',
          features: [],
        },
      });

      const carIconSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="14" height="14"><path fill="white" d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.42 1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z"/></svg>`;

      const carImg = new Image(14, 14);
      carImg.onload = () => {
        map.addImage('car-icon', carImg);

        map.addLayer({
          id: VEHICLE_LAYER_ID,
          type: 'circle',
          source: VEHICLE_SOURCE_ID,
          paint: {
            'circle-radius': 12,
            'circle-color': [
              'match',
              ['get', 'ignitionStatus'],
              'moving', '#22c55e',
              'idling', '#f59e0b',
              '#64748b',
            ],
            'circle-stroke-width': 2,
            'circle-stroke-color': '#ffffff',
          },
        });

        map.addLayer({
          id: `${VEHICLE_LAYER_ID}-car-icon`,
          type: 'symbol',
          source: VEHICLE_SOURCE_ID,
          layout: {
            'icon-image': 'car-icon',
            'icon-size': 1,
            'icon-anchor': 'center',
            'icon-allow-overlap': true,
          },
        });

        map.addLayer({
          id: `${VEHICLE_LAYER_ID}-labels`,
          type: 'symbol',
          source: VEHICLE_SOURCE_ID,
          layout: {
            'text-field': ['get', 'registration'],
            'text-size': 11,
            'text-offset': [0, 1.4],
            'text-anchor': 'top',
          },
          paint: {
            'text-color': '#1e293b',
            'text-halo-color': '#ffffff',
            'text-halo-width': 1,
          },
        });

        mapLoadedRef.current = true;
        updateVehicleMarkers(map, vehiclesRef.current);
        requestAnimationFrame(() => {
          map.resize();
        });
      };
      carImg.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(carIconSvg)}`;
    });

    mapRef.current = map;

    return () => {
      mapLoadedRef.current = false;
      map.remove();
      mapRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const container = mapContainerRef.current;
    if (!map || !container) {
      return;
    }

    const observer = new ResizeObserver(() => {
      resizeMap();
    });
    observer.observe(container);

    requestAnimationFrame(() => {
      requestAnimationFrame(resizeMap);
    });

    return () => {
      observer.disconnect();
    };
  }, [layoutKey, resizeMap]);

  useEffect(() => {
    const map = mapRef.current;
    if (!map || !mapLoadedRef.current) {
      return;
    }

    updateVehicleMarkers(map, vehiclesRef.current);
  }, [vehicles]);

  return (
    <div
      className={className}
      style={{ position: 'relative', width: '100%', height: '100%', minHeight: 0, ...style }}
    >
      <div ref={mapContainerRef} style={{ position: 'absolute', inset: 0 }} />
    </div>
  );
};

function updateVehicleMarkers(map: mapboxgl.Map, vehicles: Vehicle[]) {
  const locatedVehicles = vehicles.filter(vehicleHasLocation);
  const features = locatedVehicles.map((vehicle) => {
    const coordinates = getVehicleCoordinates(vehicle)!;

    return {
      type: 'Feature' as const,
      geometry: {
        type: 'Point' as const,
        coordinates,
      },
      properties: {
        id: vehicle.id,
        registration: vehicle.registrationNumber,
        label: getVehicleDisplayName(vehicle),
        ignitionStatus: vehicle.ignitionStatus,
        positionDescription: vehicle.status?.location?.positionDescription ?? '',
      },
    };
  });

  const source = map.getSource(VEHICLE_SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
  source?.setData({
    type: 'FeatureCollection',
    features,
  });

  if (features.length === 0) {
    map.flyTo({ center: DEFAULT_CENTER, zoom: DEFAULT_ZOOM });
    return;
  }

  if (features.length === 1) {
    map.flyTo({
      center: features[0].geometry.coordinates as [number, number],
      zoom: 14,
    });
    map.resize();
    return;
  }

  const bounds = new mapboxgl.LngLatBounds();
  for (const feature of features) {
    bounds.extend(feature.geometry.coordinates as [number, number]);
  }

  map.fitBounds(bounds, {
    padding: 80,
    maxZoom: 14,
    duration: 800,
  });

  map.resize();
}

export default AppMap;
