import React, { Component } from 'react';
import logo from './logo_32.png';
import './style.css';
import NavBarLinks, { NavBarLinksItem } from './NavBarLinks';

export class NavBar extends Component {
  readonly backgroundColor: string = "white";
  readonly title: string = "Lokman admin panel";
  readonly links: NavBarLinksItem[] = [
    { content: "Swagger", url: "/swagger/", openNewTab: false },
    { content: <span><span class='hidden lg:inline-block text-2xl'><i class='fa fa-fw fa-github text-base'></i></span><span class='lg:hidden'>GitHub</span></span>, url: "https://github.com/vchirikov/lokman", openNewTab: true },
  ]

  public render(): JSX.Element {
    return (
      <header class={`lg:px-16 px-6 bg-${this.backgroundColor} flex flex-wrap items-center py-2 lg:py-0 shadow-md`}>
        <div class="flex justify-between items-center">
          <a href={process.env.PUBLIC_URL}><span class="text-2xl"><img class="w-6 h-6" src={logo} alt="🔒" /></span></a>
        </div>
        <div class="flex-1 font-semibold px-2 lg:px-4 lg:text-xl"><a href={process.env.PUBLIC_URL}>{this.title}</a></div>
        <label htmlFor="navbar-toggle" class="cursor-pointer lg:hidden block">
          <svg class="fill-current text-gray-900" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">
            <title>menu</title>
            <path d="M0 3h20v2H0V3zm0 6h20v2H0V9zm0 6h20v2H0v-2z"></path>
          </svg>
        </label>
        <input id="navbar-toggle" class="hidden" type="checkbox" />
        <div id="navbar-links" class="hidden lg:flex lg:items-center lg:w-auto w-full">
          <NavBarLinks items={this.links} />
        </div>
      </header >);
  }
}

export default NavBar;
