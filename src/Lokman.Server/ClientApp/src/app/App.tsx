import React, { Component } from 'react';
import NavBar from 'components/NavBar/NavBar';
import LocksPage from 'components/LocksPage/LocksPage';

export class App extends Component {
  public render(): JSX.Element {
    return (
      <div>
        <NavBar />
        <div class="container mx-auto">
          <LocksPage />
        </div>
      </div>
    );
  }
}

export default App;
