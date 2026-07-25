import React from 'react';
import { IonContent, IonPage } from '@ionic/react';
import { Button, Card, Text, Persona, tokens } from '@fluentui/react-components';
import { SignOut24Regular } from '@fluentui/react-icons';
import { useHistory } from 'react-router-dom';
import { setAuthToken } from '../services/api';

const muted = { color: tokens.colorNeutralForeground3 };

const Profile: React.FC = () => {
  const history = useHistory();

  const handleSignOut = () => {
    localStorage.removeItem('token');
    setAuthToken(null);
    history.replace('/login');
  };

  return (
    <IonPage>
      <IonContent className="ion-padding app-page">
        <div className="app-page-header">
          <Text as="h1" weight="semibold" size={600}>
            Profile
          </Text>
          <Text style={muted}>Your account for remote leave and claims</Text>
        </div>

        <Card className="app-card">
          <Persona
            name="Employee"
            secondaryText="Remote access"
            size="extra-large"
            textAlignment="center"
          />
          <div style={{ marginTop: 16, textAlign: 'center' }}>
            <Text size={200} block style={muted}>
              Signed in to Chronos Leave & Claims
            </Text>
          </div>
        </Card>

        <Card className="app-card">
          <Text weight="semibold" block>
            Preferences
          </Text>
          <Text size={200} block style={{ ...muted, marginTop: 8 }}>
            Notifications and contact details will appear here once connected to your
            organisation profile.
          </Text>
        </Card>

        <Button
          appearance="secondary"
          icon={<SignOut24Regular />}
          onClick={handleSignOut}
          style={{ width: '100%', marginTop: 8 }}
        >
          Sign out
        </Button>
      </IonContent>
    </IonPage>
  );
};

export default Profile;
