import React, { useState } from 'react';
import { IonContent, IonPage } from '@ionic/react';
import {
  Button,
  Card,
  CardHeader,
  Text,
  Badge,
  tokens,
} from '@fluentui/react-components';
import { CalendarClock24Regular, Add24Filled } from '@fluentui/react-icons';
import { useHistory } from 'react-router-dom';
import './Dashboard.css';

const muted = { color: tokens.colorNeutralForeground3 };

const Dashboard: React.FC = () => {
  const history = useHistory();
  const [requests] = useState([
    { id: 1, type: 'Annual Leave', dates: 'Aug 10 - Aug 14', status: 'Pending', color: 'warning' as const },
    { id: 2, type: 'Sick Leave', dates: 'Jul 02 - Jul 03', status: 'Approved', color: 'success' as const },
  ]);

  return (
    <IonPage>
      <IonContent className="ion-padding app-page">
        <div className="app-page-header app-page-header-row">
          <div>
            <Text as="h1" weight="semibold" size={600}>
              Dashboard
            </Text>
            <Text style={muted}>Leave balances and recent activity</Text>
          </div>
          <Button
            size="large"
            appearance="primary"
            icon={<Add24Filled />}
            aria-label="Apply"
            style={{ borderRadius: 40 }}
            onClick={() => history.push('/tabs/apply')}
          />
        </div>

        <Card className="app-card">
          <CardHeader
            image={<CalendarClock24Regular />}
            header={<Text weight="semibold">Annual balance</Text>}
            description={<Text style={muted}>Resets on Jan 1st</Text>}
          />
          <div style={{ padding: '10px 0' }}>
            <Text size={900} weight="bold">
              14.5 Days
            </Text>
            <br />
            <Text size={200} style={muted}>
              Available to book
            </Text>
          </div>
        </Card>

        <Text as="h2" weight="medium" size={400} style={{ display: 'block', marginBottom: 12 }}>
          Recent requests
        </Text>

        {requests.map((req) => (
          <Card key={req.id} className="app-card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <Text weight="semibold" block>
                  {req.type}
                </Text>
                <Text size={200} style={muted}>
                  {req.dates}
                </Text>
              </div>
              <Badge appearance="filled" color={req.color}>
                {req.status}
              </Badge>
            </div>
          </Card>
        ))}
      </IonContent>
    </IonPage>
  );
};

export default Dashboard;
