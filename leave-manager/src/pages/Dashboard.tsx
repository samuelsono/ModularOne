import React, { useCallback, useEffect, useState } from 'react';
import {
  IonAvatar,
  IonButton,
  IonContent,
  IonIcon,
  IonItem,
  IonPage,
  IonRefresher,
  IonRefresherContent,
  IonSelect,
  IonSelectOption,
  RefresherEventDetail,
} from '@ionic/react';
import {
  Button,
  Card,
  CardHeader,
  Text,
  Badge,
  Spinner,
  tokens,
  Persona,
} from '@fluentui/react-components';
import { CalendarClock24Regular, Add24Filled, CalendarRegular } from '@fluentui/react-icons';
import LeaveHistoryList from '../components/LeaveHistoryList';
import { loadLeaveDashboard } from '../services/leaveDashboardService';
import { displayNameForUser, getStoredUser } from '../services/authService';
import type { LeaveBalance, LeaveHistoryItem } from '../types/leave';
import '../main.css';
import { heart } from 'ionicons/icons';

const muted = { color: tokens.colorNeutralForeground3 };

function formatDays(value: number): string {
  return Number.isInteger(value) ? String(value) : value.toFixed(1);
}

function badgeColor(status: string): 'warning' | 'success' | 'danger' | 'informative' {
  switch (status.toLowerCase()) {
    case 'approved':
      return 'success';
    case 'rejected':
      return 'danger';
    case 'cancelled':
      return 'informative';
    default:
      return 'warning';
  }
}

function formatDate(value: string): string {
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) {
    return value || '—';
  }
  return new Intl.DateTimeFormat(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(parsed));
}

function formatShortRange(startDate: string, endDate: string): string {
  const format = (value: string) => {
    const parsed = Date.parse(value);
    if (Number.isNaN(parsed)) {
      return value || '—';
    }
    return new Intl.DateTimeFormat(undefined, {
      day: 'numeric',
      month: 'short',
    }).format(new Date(parsed));
  };
  const start = format(startDate);
  const end = format(endDate);
  return start === end ? start : `${start} – ${end}`;
}

function pickPrimaryBalance(balances: LeaveBalance[]): LeaveBalance | null {
  if (balances.length === 0) {
    return null;
  }
  const annual = balances.find((balance) => /annual/i.test(balance.leaveTypeName));
  return annual ?? balances[0];
}

function readErrorMessage(err: unknown): string {
  const axiosMessage = (err as { response?: { data?: { message?: string; title?: string } } })
    ?.response?.data;
  return (
    axiosMessage?.message ||
    axiosMessage?.title ||
    (err as { message?: string })?.message ||
    'Failed to load leave dashboard.'
  );
}

const Dashboard: React.FC = () => {
  const user = getStoredUser();
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [upcoming, setUpcoming] = useState<LeaveHistoryItem[]>([]);
  const [historyItems, setHistoryItems] = useState<LeaveHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await loadLeaveDashboard();
      setBalances(data.balances);
      setUpcoming(data.upcomingRequests);
      setHistoryItems(data.history);
    } catch (err: unknown) {
      setError(readErrorMessage(err));
      setBalances([]);
      setUpcoming([]);
      setHistoryItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const handleRefresh = async (event: CustomEvent<RefresherEventDetail>) => {
    setError(null);
    try {
      const data = await loadLeaveDashboard();
      setBalances(data.balances);
      setUpcoming(data.upcomingRequests);
      setHistoryItems(data.history);
    } catch (err: unknown) {
      setError(readErrorMessage(err));
      setBalances([]);
      setUpcoming([]);
      setHistoryItems([]);
    } finally {
      event.detail.complete();
    }
  };

  const primaryBalance = pickPrimaryBalance(balances);

  return (
    <IonPage>
      <IonContent className="ion-padding app-page">
        <IonRefresher slot="fixed" onIonRefresh={handleRefresh}>
          <IonRefresherContent />
        </IonRefresher>

        <div className="flex justify-between items-center p-5">
          <Persona
            avatar={{ color: 'colorful', idForColor: user?.email || user?.username || 'user' }}
            size="large"
            name={displayNameForUser(user)}
            secondaryText={user?.email || 'Leave balances and recent activity'}
          />
        </div>

          <IonSelect label="Year"  placeholder="Select year" className="bg-white! px-5 mb-1">
            <IonSelectOption value="2023">2023</IonSelectOption>
            <IonSelectOption value="2024">2024</IonSelectOption>
            <IonSelectOption value="2025">2025</IonSelectOption>
          </IonSelect>

        {loading && (
          <Card className="app-card">
            <Spinner size="small" label="Loading dashboard…" />
          </Card>
        )}

        {!loading && error && (
          <Card className="app-card">
            <Text block style={{ color: tokens.colorPaletteRedForeground1 }}>
              {error}
            </Text>
            <Button appearance="secondary" onClick={() => void loadDashboard()} style={{ marginTop: 12 }}>
              Retry
            </Button>
          </Card>
        )}

        {!loading && !error && (
          <>
            <Card className="app-card shadow-none! mb-3">
              <CardHeader
                image={<CalendarRegular fontSize={35} className='text-gray-400' />}
                header={
                  <Text weight="semibold">
                    {primaryBalance ? `${primaryBalance.leaveTypeName} balance` : 'Leave balance'}
                  </Text>
                }
                description={
                  <Text style={muted}>
                    {primaryBalance
                      ? `Cycle ends ${formatDate(primaryBalance.cycleEnd)}`
                      : 'No balances available'}
                  </Text>
                }
              />
             
            </Card>

            {balances.length > 1 && (
              <div className="balance-chip-row grid grid-cols-2 gap-3 mb-3 p-3">
                {balances.map((balance, index) => (
                  <Card key={balance.id} className={"flex flex-col gap-0! rounded-xl! shadow-xs! " + (index === 0 ? "col-span-2 md:col-span-1" : "col-span-1")}>
                    <div className="flex justify-between">
                    <Text size={300} block>
                      {balance.leaveTypeName}
                    </Text>
                    <span className='size-3 rounded-full' style={{ backgroundColor: balance.leaveTypeColor ?? "" }}></span>
                    </div>
                    <div className="flex justify-between">
                      <Text className="font-thin!" size={600} block>
                      {formatDays(balance.used)}
                    </Text>
                    <Text className="font-thin!" size={800} block>
                      {formatDays(balance.remaining)}
                    </Text>
                    </div>
                    <div className="flex justify-between">
                      <Text size={300} style={muted}>
                      used
                    </Text>
                    <Text size={300} style={muted}>
                      balance
                    </Text>
                    </div>
                  </Card>
                ))}
              </div>
            )}

            <Text as="h2" weight="medium" size={400} style={{ display: 'block', margin: 12 }}>
              Upcoming requests
            </Text>

            {upcoming.length === 0 ? (
              <Card className="app-card shadow-xs! mb-3">
                <Text style={muted}>No upcoming leave requests.</Text>
              </Card>
            ) : (
              upcoming.map((req) => (
                <Card key={req.id} className="app-card shadow-xs!">
                  <div className="leave-history-row">
                    <div>
                      <Text weight="semibold" block>
                        {req.type}
                      </Text>
                      <Text size={200} style={muted}>
                        {formatShortRange(req.startDate, req.endDate)}
                      </Text>
                    </div>
                    <Badge appearance="filled" color={badgeColor(String(req.status))}>
                      {req.status}
                    </Badge>
                  </div>
                </Card>
              ))
            )}

            <LeaveHistoryList items={historyItems} onRetry={() => void loadDashboard()} />
          </>
        )}
      </IonContent>
    </IonPage>
  );
};

export default Dashboard;
