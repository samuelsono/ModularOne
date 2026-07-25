import React from 'react';
import { Redirect, Route } from 'react-router-dom';
import {
  IonIcon,
  IonLabel,
  IonRouterOutlet,
  IonTabBar,
  IonTabButton,
  IonTabs,
} from '@ionic/react';
import { calendarOutline, personOutline, addCircleOutline } from 'ionicons/icons';
import Dashboard from './Dashboard';
import Profile from './Profile';
import Apply from './Apply';

const MainTabs: React.FC = () => {
  return (
    <IonTabs>
      <IonRouterOutlet>
        <Route exact path="/tabs/dashboard" component={Dashboard} />
        <Route exact path="/tabs/profile" component={Profile} />
        <Route exact path="/tabs/apply" component={Apply} />
        <Route exact path="/tabs">
          <Redirect to="/tabs/dashboard" />
        </Route>
      </IonRouterOutlet>

      <IonTabBar slot="bottom" className="app-tab-bar">
        <IonTabButton tab="dashboard" href="/tabs/dashboard">
          <IonIcon icon={calendarOutline} />
          <IonLabel>Dashboard</IonLabel>
        </IonTabButton>
        <IonTabButton tab="apply" href="/tabs/apply">
          <IonIcon icon={addCircleOutline} />
          <IonLabel>Apply</IonLabel>
        </IonTabButton>
        <IonTabButton tab="profile" href="/tabs/profile">
          <IonIcon icon={personOutline} />
          <IonLabel>Profile</IonLabel>
        </IonTabButton>
      </IonTabBar>
    </IonTabs>
  );
};

export default MainTabs;
