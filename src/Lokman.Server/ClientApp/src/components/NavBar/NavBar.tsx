import React, { Component } from 'react';
import logo from './logo_32.png';
import './style.css';

export class NavBar extends Component {
  readonly backgroundColor: string = "white";
  readonly borderColor: string = "blue";
  readonly title: string = "Lokman admin panel";
  readonly linkCssClasses = `py-1 px-0 lg:px-2 block border-b-2 border-transparent hover:border-${this.borderColor}-400`;

  public render(): JSX.Element {
    return (
      <header className={`lg:px-16 px-6 bg-${this.backgroundColor} flex flex-wrap items-center py-2 lg:py-0 shadow-md`}>
        <div className="flex justify-between items-center">
          <a href={process.env.PUBLIC_URL}><span className="text-2xl"><img className="w-6 h-6" src={logo} alt="🔒" /></span></a>
        </div>
        <div className="flex-1 font-semibold px-2 lg:px-4 lg:text-xl"><a href={process.env.PUBLIC_URL}>{this.title}</a></div>
        <label htmlFor="navbar-toggle" className="cursor-pointer lg:hidden block">
          <svg className="fill-current text-gray-900" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
            <title>menu</title>
            <path d="M0 3h20v2H0V3zm0 6h20v2H0V9zm0 6h20v2H0v-2z"></path>
          </svg>
        </label>
        <input id="navbar-toggle" className="hidden" type="checkbox" />
        <div id="navbar-links" className="hidden lg:flex lg:items-center lg:w-auto w-full">
          <nav>
            <ul className="lg:flex items-center justify-between text-gray-700 pt-3 lg:pt-0 text-xl">
              <li><a className={this.linkCssClasses} href={`${process.env.PUBLIC_URL}/swagger/`} target="_self">Swagger</a></li>
              <li>
                <a className={this.linkCssClasses} href="https://github.com/vchirikov/lokman" rel="nofollow noopener noreferrer" target="_blank">
                  <span className='hidden lg:inline-block text-2xl'><i className='fa fa-fw fa-github text-base'></i></span><span className='lg:hidden'>GitHub</span>
                </a>
              </li>
            </ul>
          </nav>
        </div>
      </header >);
  }
}
export default NavBar;
