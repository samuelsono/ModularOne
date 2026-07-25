import React from 'react';
import { IonContent, IonPage } from '@ionic/react';
import { Card, Text, tokens } from '@fluentui/react-components';
import {
  CalendarAdd24Regular,
  MoneyHand24Regular,
  ChevronRight24Regular,
} from '@fluentui/react-icons';

const muted = { color: tokens.colorNeutralForeground3 };

type ApplyAction = {
  id: string;
  title: string;
  description: string;
  icon: React.ReactNode;
  onClick: () => void;
};

const Apply: React.FC = () => {
  const actions: ApplyAction[] = [
    {
      id: 'leave',
      title: 'Apply for leave',
      description: 'Request time off while working remotely',
      icon: <CalendarAdd24Regular />,
      onClick: () => alert('Leave application form coming soon'),
    },
    {
      id: 'claim',
      title: 'Submit a claim',
      description: 'Capture expenses and reimbursements on the go',
      icon: <MoneyHand24Regular />,
      onClick: () => alert('Claims form coming soon'),
    },
  ];

  return (
    <IonPage>
      <IonContent className="ion-padding app-page">
        <div className="app-page-header">
          <Text as="h1" weight="semibold" size={600}>
            Apply
          </Text>
          <Text style={muted}>Leave and claims when you are away from the office</Text>
        </div>

        {actions.map((action) => (
          <button
            key={action.id}
            type="button"
            className="app-action-tile"
            onClick={action.onClick}
          >
            <Card className="app-card app-action-card">
              <div className="app-action-row">
                <div className="app-action-icon">{action.icon}</div>
                <div className="app-action-copy">
                  <Text weight="semibold" block>
                    {action.title}
                  </Text>
                  <Text size={200} block style={muted}>
                    {action.description}
                  </Text>
                </div>
                <ChevronRight24Regular className="app-action-chevron" />
              </div>
            </Card>
          </button>
        ))}
      </IonContent>
    </IonPage>
  );
};

export default Apply;
