import { useCallback, useEffect, useRef } from 'react';
import * as mapboxgl from 'mapbox-gl/esm';
import { MAPBOX_ACCESS_TOKEN } from '../../config/mapbox';
import type { ReportExecutionResult } from '../../types/report';
import 'mapbox-gl/dist/mapbox-gl.css';

const SOURCE_ID = 'map-report-pois';
const LAYER_ID = 'map-report-layer';
const DEFAULT_CENTER: [number, number] = [28.1005628, -25.894096];
const DEFAULT_ZOOM = 6;

// Column indices matching ExecuteMapAsync response
const COL_REGISTRATION = 0;
const COL_LAT = 1;
const COL_LNG = 2;
const COL_MAKE = 3;
const COL_MODEL = 4;
const COL_IGNITION = 5;
const COL_POSITION = 6;
const COL_SPEED = 7;

interface MapWidgetProps {
  data: ReportExecutionResult;
}

function buildFeatures(data: ReportExecutionResult) {
  return data.rows
    .filter((row) => {
      const lat = row[COL_LAT];
      const lng = row[COL_LNG];
      return lat != null && lng != null && typeof lat === 'number' && typeof lng === 'number';
    })
    .map((row) => ({
      type: 'Feature' as const,
      geometry: {
        type: 'Point' as const,
        coordinates: [row[COL_LNG] as number, row[COL_LAT] as number],
      },
      properties: {
        registration: String(row[COL_REGISTRATION] ?? ''),
        make: String(row[COL_MAKE] ?? ''),
        model: String(row[COL_MODEL] ?? ''),
        ignitionStatus: String(row[COL_IGNITION] ?? 'off'),
        positionDescription: String(row[COL_POSITION] ?? ''),
        speed: typeof row[COL_SPEED] === 'number' ? row[COL_SPEED] : null,
      },
    }));
}

export function MapWidget({ data }: MapWidgetProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<mapboxgl.Map | null>(null);
  const mapLoadedRef = useRef(false);
  const dataRef = useRef(data);
  dataRef.current = data;

  const resizeMap = useCallback(() => {
    mapRef.current?.resize();
  }, []);

  // Initialise the map once
  useEffect(() => {
    if (!containerRef.current || mapRef.current) return;

    const map = new mapboxgl.Map({
      accessToken: MAPBOX_ACCESS_TOKEN,
      container: containerRef.current,
      center: DEFAULT_CENTER,
      zoom: DEFAULT_ZOOM,
      style: 'mapbox://styles/mapbox/standard',
    });

    map.addControl(new mapboxgl.NavigationControl(), 'top-right');

    const carIconSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="14" height="14"><path fill="white" d="M18.92 6.01C18.72 5.42 18.16 5 17.5 5h-11c-.66 0-1.21.42-1.01L3 12v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 16c-.83 0-1.5-.67-1.5-1.5S5.67 13 6.5 13s1.5.67 1.5 1.5S7.33 16 6.5 16zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 11l1.5-4.5h11L19 11H5z"/></svg>`;
    const carImg = new Image(14, 14);

    map.on('load', () => {
      map.addSource(SOURCE_ID, {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
      });

      carImg.onload = () => {
        map.addImage('map-report-car', carImg);

        map.addLayer({
          id: LAYER_ID,
          type: 'circle',
          source: SOURCE_ID,
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
          id: `${LAYER_ID}-icon`,
          type: 'symbol',
          source: SOURCE_ID,
          layout: {
            'icon-image': 'map-report-car',
            'icon-size': 1,
            'icon-anchor': 'center',
            'icon-allow-overlap': true,
          },
        });

        map.addLayer({
          id: `${LAYER_ID}-labels`,
          type: 'symbol',
          source: SOURCE_ID,
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
        updateMarkers(map, dataRef.current);
        requestAnimationFrame(() => map.resize());
      };
      carImg.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(carIconSvg)}`;

      map.on('click', LAYER_ID, (e) => {
        if (!e.features?.length) return;
        // cast through unknown — Mapbox v3 GeoJSONFeature misses geometry/properties in type defs
        const feature = e.features[0] as unknown as {
          properties: Record<string, string | number | null>;
          geometry: { coordinates: number[] };
        };
        const props = feature.properties ?? {};
        const coords = feature.geometry.coordinates.slice() as [number, number];
        const speedKmh = props.speed != null ? `${(props.speed as number).toFixed(1)} km/h` : '—';
        const vehicle = `${String(props.make ?? '')} ${String(props.model ?? '')}`.trim() || '—';
        new mapboxgl.Popup({ offset: 16 })
          .setLngLat(coords)
          .setHTML(
            `<strong>${String(props.registration ?? '')}</strong><br/>` +
            `${vehicle}<br/>` +
            `Status: ${String(props.ignitionStatus ?? '')}<br/>` +
            `Speed: ${speedKmh}` +
            (props.positionDescription ? `<br/><span style="font-size:11px">${String(props.positionDescription)}</span>` : ''),
          )
          .addTo(map);
      });

      map.on('mouseenter', LAYER_ID, () => { map.getCanvas().style.cursor = 'pointer'; });
      map.on('mouseleave', LAYER_ID, () => { map.getCanvas().style.cursor = ''; });
    });

    mapRef.current = map;

    return () => {
      mapLoadedRef.current = false;
      map.remove();
      mapRef.current = null;
    };
  }, []);

  // Respond to container resize
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;
    const observer = new ResizeObserver(resizeMap);
    observer.observe(container);
    requestAnimationFrame(() => requestAnimationFrame(resizeMap));
    return () => observer.disconnect();
  }, [resizeMap]);

  // Update markers when data changes
  useEffect(() => {
    if (mapRef.current && mapLoadedRef.current) {
      updateMarkers(mapRef.current, data);
    }
  }, [data]);

  return (
    <div style={{ position: 'relative', width: '100%', height: '100%', minHeight: '300px' }}>
      <div ref={containerRef} style={{ position: 'absolute', inset: 0 }} />
    </div>
  );
}

function updateMarkers(map: mapboxgl.Map, data: ReportExecutionResult) {
  const features = buildFeatures(data);
  const source = map.getSource(SOURCE_ID) as mapboxgl.GeoJSONSource | undefined;
  source?.setData({ type: 'FeatureCollection', features });

  if (features.length === 0) {
    map.flyTo({ center: DEFAULT_CENTER, zoom: DEFAULT_ZOOM });
    return;
  }

  if (features.length === 1) {
    map.flyTo({ center: features[0].geometry.coordinates as [number, number], zoom: 14 });
    map.resize();
    return;
  }

  const bounds = new mapboxgl.LngLatBounds();
  for (const feature of features) {
    bounds.extend(feature.geometry.coordinates as [number, number]);
  }
  map.fitBounds(bounds, { padding: 60, maxZoom: 14, duration: 800 });
}
